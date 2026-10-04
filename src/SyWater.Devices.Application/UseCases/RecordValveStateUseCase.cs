using SyWater.Devices.Application.Events;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Application.Telemetry;
using SyWater.Devices.Application.Valve;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

public sealed class RecordValveStateUseCase(IDeviceRepository devices, IEventPublisher events, TimeProvider clock)
    : IRecordValveStateUseCase
{
    public async Task<MessageOutcome> ExecuteAsync(ValveStateMessage message, CancellationToken ct)
    {
        var device = await devices.GetBySerialAsync(message.SerialNumber, ct);
        if (device is null) return MessageOutcome.UnknownDevice;

        var now = clock.GetUtcNow().UtcDateTime;
        var auth = device.RecordHeartbeat(message.Token, firmwareVersion: null, now);
        if (auth != HeartbeatOutcome.Accepted) return MessageOutcome.From(auth);
        await devices.SaveHeartbeatAsync(device, ct);

        if (device.PlaceId is not { } placeId) return MessageOutcome.NotLinked;

        // The server time, not the device's: an ESP32 without NTP must not confirm "in the past"
        await events.PublishAsync(new ValveReported(device.Id, placeId, message.State, message.CommandId, now), ct);
        return MessageOutcome.Accepted;
    }
}
