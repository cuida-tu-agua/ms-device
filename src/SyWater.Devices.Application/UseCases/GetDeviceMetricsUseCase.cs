using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;

namespace SyWater.Devices.Application.UseCases;

public sealed class GetDeviceMetricsUseCase(IDeviceRepository devices, TimeProvider clock) : IGetDeviceMetricsUseCase
{
    public Task<DeviceMetrics> ExecuteAsync(CancellationToken ct) =>
        devices.GetMetricsAsync(clock.GetUtcNow().UtcDateTime, ct);
}
