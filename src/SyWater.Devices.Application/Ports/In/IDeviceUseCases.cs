using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Heartbeats;
using SyWater.Devices.Application.Telemetry;
using SyWater.Devices.Application.Valve;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Ports.In;

// Inbound ports: what the outside world (HTTP controllers, MQTT listener, background jobs) can ask for.

public interface ILinkDeviceUseCase
{
    Task<DeviceView> ExecuteAsync(LinkDeviceCommand command, CancellationToken ct);
}

public interface IGetPlaceDeviceUseCase
{
    Task<DeviceView> ExecuteAsync(Guid userId, Guid placeId, CancellationToken ct);
}

public interface IUnlinkDeviceUseCase
{
    Task ExecuteAsync(Guid userId, Guid placeId, CancellationToken ct);
}

public interface IRecordHeartbeatUseCase
{
    Task<HeartbeatOutcome> ExecuteAsync(HeartbeatMessage message, CancellationToken ct);
}

public interface IRefreshDeviceStatusUseCase
{
    Task<int> ExecuteAsync(CancellationToken ct);
}

public interface IRecordTelemetryUseCase
{
    Task<MessageOutcome> ExecuteAsync(TelemetryMessage message, CancellationToken ct);
}

public interface IRecordValveStateUseCase
{
    Task<MessageOutcome> ExecuteAsync(ValveStateMessage message, CancellationToken ct);
}

public interface ISendValveCommandUseCase
{
    Task<ValveCommandSent> ExecuteAsync(Guid userId, Guid placeId, Guid commandId, ValveAction action, CancellationToken ct);
}
