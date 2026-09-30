using SyWater.Devices.Application.Heartbeats;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

/// <summary>
/// A heartbeat from the broker. Returns an outcome instead of throwing: MQTT has nobody to
/// answer to, the listener only writes a log line.
/// </summary>
public sealed class RecordHeartbeatUseCase(IDeviceRepository devices, TimeProvider clock) : IRecordHeartbeatUseCase
{
    public async Task<HeartbeatOutcome> ExecuteAsync(HeartbeatMessage message, CancellationToken ct)
    {
        var device = await devices.GetBySerialAsync(message.SerialNumber, ct);
        if (device is null) return HeartbeatOutcome.UnknownDevice;

        var outcome = device.RecordHeartbeat(message.Token, message.FirmwareVersion, clock.GetUtcNow().UtcDateTime);
        if (outcome == HeartbeatOutcome.Accepted)
            await devices.SaveHeartbeatAsync(device, ct);

        return outcome;
    }
}
