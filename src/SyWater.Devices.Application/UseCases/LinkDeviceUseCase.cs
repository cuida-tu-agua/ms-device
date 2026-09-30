using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

/// <summary>
/// HU-012. The order of the checks matters:
///   1. formats (cheap, no I/O)          → 400
///   2. the place is mine (ms-places)    → 404
///   3. the place has no other device    → 409
///   4. serial + pairing code            → 400 (same error for both) / 429 (device locked)
///   5. device free, not revoked, online → 409
/// Steps 4-5 never run for a place that is not yours, so nobody can probe devices.
/// </summary>
public sealed class LinkDeviceUseCase(
    IDeviceRepository devices,
    IPlaceOwnershipChecker places,
    DevicePolicy policy,
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

        // Right code. The failed attempts are cleared only when the link is saved.
        var link = device.LinkTo(command.PlaceId, command.UserId, now, policy.RequireOnlineToLink);
        await devices.LinkAsync(device, link, ct);

        return DeviceView.From(device, now);
    }
}
