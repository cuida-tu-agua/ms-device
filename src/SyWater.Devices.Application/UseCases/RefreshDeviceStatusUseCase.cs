using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;

namespace SyWater.Devices.Application.UseCases;

/// <summary>
/// HU-013. Keeps the status column honest for other readers (reports, consumption-service).
/// The API does not depend on it: DeviceView calculates the status on every read.
/// </summary>
public sealed class RefreshDeviceStatusUseCase(IDeviceRepository devices, TimeProvider clock) : IRefreshDeviceStatusUseCase
{
    public Task<int> ExecuteAsync(CancellationToken ct) =>
        devices.MarkStaleAsDisconnectedAsync(clock.GetUtcNow().UtcDateTime, ct);
}
