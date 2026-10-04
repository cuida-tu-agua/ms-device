using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Application.Valve;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

public sealed class SendValveCommandUseCase(IDeviceRepository devices, IValveCommandSender sender, TimeProvider clock)
    : ISendValveCommandUseCase
{
    public async Task<ValveCommandSent> ExecuteAsync(
        Guid userId, Guid placeId, Guid commandId, ValveAction action, CancellationToken ct)
    {
        var device = await devices.GetByPlaceAsync(placeId, ct);
        if (device is null || !device.IsLinkedBy(userId))
            throw new DeviceNotLinkedException(placeId);

        var now = clock.GetUtcNow().UtcDateTime;
        if (device.StatusAt(now) != DeviceStatus.Connected)
            throw new DeviceOfflineException(device.SerialNumber);

        await sender.SendAsync(device.SerialNumber, commandId, action, ct);
        return new ValveCommandSent(commandId, device.SerialNumber, now);
    }
}
