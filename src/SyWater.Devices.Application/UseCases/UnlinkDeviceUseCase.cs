using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

/// <summary>
/// HU-014. The device row loses its place and the history row gets unlinked_at/unlinked_by.
/// Nothing is deleted: the consumption already recorded stays with the place.
/// </summary>
public sealed class UnlinkDeviceUseCase(IDeviceRepository devices, TimeProvider clock) : IUnlinkDeviceUseCase
{
    public async Task ExecuteAsync(Guid userId, Guid placeId, CancellationToken ct)
    {
        var device = await devices.GetByPlaceAsync(placeId, ct);
        if (device is null || !device.IsLinkedBy(userId))
            throw new DeviceNotLinkedException(placeId);

        var now = clock.GetUtcNow().UtcDateTime;
        device.Unlink(now);
        await devices.UnlinkAsync(device, placeId, userId, now, ct);
    }
}
