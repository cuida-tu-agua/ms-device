using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

/// <summary>
/// HU-013. Only the user who linked the device can see it. For anyone else the answer is the
/// same as "no device" (404), so the endpoint does not reveal other people's places.
/// </summary>
public sealed class GetPlaceDeviceUseCase(IDeviceRepository devices, TimeProvider clock) : IGetPlaceDeviceUseCase
{
    public async Task<DeviceView> ExecuteAsync(Guid userId, Guid placeId, CancellationToken ct)
    {
        var device = await devices.GetByPlaceAsync(placeId, ct);
        if (device is null || !device.IsLinkedBy(userId))
            throw new DeviceNotLinkedException(placeId);

        return DeviceView.From(device, clock.GetUtcNow().UtcDateTime);
    }
}
