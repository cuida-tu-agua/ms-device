using SyWater.Devices.Application.Events;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

public sealed class UnlinkDeviceUseCase(IDeviceRepository devices, IEventPublisher events, TimeProvider clock)
    : IUnlinkDeviceUseCase
{
    public async Task ExecuteAsync(Guid userId, Guid placeId, CancellationToken ct)
    {
        var device = await devices.GetByPlaceAsync(placeId, ct);
        if (device is null || !device.IsLinkedBy(userId))
            throw new DeviceNotLinkedException(placeId);

        var now = clock.GetUtcNow().UtcDateTime;
        device.Unlink(now);
        await devices.UnlinkAsync(device, placeId, userId, now, ct);

        // After the commit: valve-service forgets this valve (consumption keeps the history)
        await events.PublishAsync(new DeviceUnlinked(device.Id, device.SerialNumber, placeId, userId, now), ct);
    }
}