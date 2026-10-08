using SyWater.Devices.Application.UseCases;

namespace SyWater.Devices.Application.Tests;

/// <summary>HU-062: the device numbers of the administrator's dashboard.</summary>
public class DeviceMetricsTests
{
    private readonly FakeDeviceRepository _devices = new();
    private readonly FakeClock _clock = new(TestDevices.Now);

    [Fact]
    public async Task Counts_total_connected_now_and_linked()
    {
        _devices.Devices.AddRange([
            TestDevices.New("SW-ESP32-000001", TestDevices.Now.AddSeconds(-20), Guid.NewGuid(), Guid.NewGuid()), // linked, online
            TestDevices.New("SW-ESP32-000002", TestDevices.Now.AddHours(-3), Guid.NewGuid(), Guid.NewGuid()),    // linked, silent too long
            TestDevices.New("SW-ESP32-000003", TestDevices.Now.AddSeconds(-5)),                                  // free, online
            TestDevices.New("SW-ESP32-000004"),                                                                  // never reported
        ]);

        var metrics = await new GetDeviceMetricsUseCase(_devices, _clock).ExecuteAsync(default);

        Assert.Equal(4, metrics.Total);
        Assert.Equal(2, metrics.Connected);   // calculated now, not the stored status
        Assert.Equal(2, metrics.Linked);
    }

    [Fact]
    public async Task With_no_devices_everything_is_zero()
    {
        var metrics = await new GetDeviceMetricsUseCase(_devices, _clock).ExecuteAsync(default);

        Assert.Equal((0, 0, 0), (metrics.Total, metrics.Connected, metrics.Linked));
    }
}
