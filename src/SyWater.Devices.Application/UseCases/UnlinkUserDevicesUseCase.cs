using SyWater.Devices.Application.Events;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

public sealed class UnlinkUserDevicesUseCase(IDeviceRepository devices, IEventPublisher events, TimeProvider clock)
    : IUnlinkUserDevicesUseCase
{
    public async Task<int> ExecuteAsync(Guid userId, CancellationToken ct)
    {
        var linked = await devices.GetLinkedByAsync(userId, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var unlinked = 0;

        foreach (var device in linked)
        {
            var placeId = device.PlaceId!.Value;
            try
            {
                device.Unlink(now);
                await devices.UnlinkAsync(device, placeId, userId, now, ct);
            }
            catch (DeviceNotLinkedException)
            {
                continue; // the user unlinked it a moment ago: the goal (no longer linked) is already met
            }

            // Same event as a manual unlink: valve-service forgets the valve, consumption keeps its history
            await events.PublishAsync(new DeviceUnlinked(device.Id, device.SerialNumber, placeId, userId, now), ct);
            unlinked++;
        }

        return unlinked; // calling it again is harmless: it answers 0
    }
}
