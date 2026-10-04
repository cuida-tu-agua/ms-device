using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Events;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

public sealed class LinkDeviceUseCase(
    IDeviceRepository devices,
    IPlaceOwnershipChecker places,  
    DevicePolicy policy,
    IEventPublisher events,
    TimeProvider clock) : ILinkDeviceUseCase
{
    public async Task<DeviceView> ExecuteAsync(LinkDeviceCommand command, CancellationToken ct)
    {
        var serial = DeviceSecrets.NormalizeSerial(command.SerialNumber);
        var code = DeviceSecrets.NormalizePairingCode(command.PairingCode);

        if (!await places.IsOwnedByRequesterAsync(command.PlaceId, ct))
            throw new PlaceNotFoundException(command.PlaceId);

        var current = await devices.GetByPlaceAsync(command.PlaceId, ct);
        if (current is not null)
        {
            throw current.SerialNumber == serial
                ? new DeviceAlreadyLinkedException(serial)
                : new PlaceAlreadyHasDeviceException(command.PlaceId);
        }

        var device = await devices.GetBySerialAsync(serial, ct)
                     ?? throw new PairingFailedException();

        var now = clock.GetUtcNow().UtcDateTime;
        device.EnsurePairingNotLocked(now);

        if (!device.PairingCodeMatches(code))
        {
            device.RegisterFailedPairing(now, policy.Pairing);
            await devices.SaveFailedPairingAsync(device, policy.Pairing, now, ct); // BEFORE throwing
            throw new PairingFailedException();
        }

        var link = device.LinkTo(command.PlaceId, command.UserId, now, policy.RequireOnlineToLink);
        await devices.LinkAsync(device, link, ct);

        await events.PublishAsync(new DeviceLinked(device.Id, device.SerialNumber, command.PlaceId, command.UserId, now), ct);

        return DeviceView.From(device, now);
    }
}
