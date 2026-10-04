using SyWater.Devices.Application.Events;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Application.Telemetry;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

public sealed class RecordTelemetryUseCase(IDeviceRepository devices, IEventPublisher events, TimeProvider clock)
    : IRecordTelemetryUseCase
{
    public async Task<MessageOutcome> ExecuteAsync(TelemetryMessage message, CancellationToken ct)
    {
        var device = await devices.GetBySerialAsync(message.SerialNumber, ct);
        if (device is null) return MessageOutcome.UnknownDevice;

        var now = clock.GetUtcNow().UtcDateTime;
        var auth = device.RecordHeartbeat(message.Token, firmwareVersion: null, now);
        if (auth != HeartbeatOutcome.Accepted) return MessageOutcome.From(auth);
        await devices.SaveHeartbeatAsync(device, ct);

        if (device.PlaceId is not { } placeId) return MessageOutcome.NotLinked;

        var problem = TelemetryRules.Validate(message, now);
        if (problem is not null) return MessageOutcome.Rejected(problem);

        await events.PublishAsync(new ReadingReceived(
            device.Id, placeId, message.RecordedAt, message.FlowLpm, message.VolumeLiters, message.TotalLiters, now), ct);
        return MessageOutcome.Accepted;
    }
}
