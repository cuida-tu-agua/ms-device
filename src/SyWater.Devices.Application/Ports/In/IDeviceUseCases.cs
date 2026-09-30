using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Heartbeats;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Ports.In;

// Inbound ports: what the outside world (HTTP controllers, MQTT listener, background jobs) can ask for.

/// <summary>HU-012: link a device to one of the user's places.</summary>
public interface ILinkDeviceUseCase
{
    Task<DeviceView> ExecuteAsync(LinkDeviceCommand command, CancellationToken ct);
}

/// <summary>HU-013: the device of a place and its status.</summary>
public interface IGetPlaceDeviceUseCase
{
    Task<DeviceView> ExecuteAsync(Guid userId, Guid placeId, CancellationToken ct);
}

/// <summary>HU-014: unlink the device of a place (history is kept).</summary>
public interface IUnlinkDeviceUseCase
{
    Task ExecuteAsync(Guid userId, Guid placeId, CancellationToken ct);
}

/// <summary>A heartbeat arrived from the MQTT broker.</summary>
public interface IRecordHeartbeatUseCase
{
    Task<HeartbeatOutcome> ExecuteAsync(HeartbeatMessage message, CancellationToken ct);
}

/// <summary>HU-013: writes DISCONNECTED on devices that stopped reporting. Returns how many changed.</summary>
public interface IRefreshDeviceStatusUseCase
{
    Task<int> ExecuteAsync(CancellationToken ct);
}
