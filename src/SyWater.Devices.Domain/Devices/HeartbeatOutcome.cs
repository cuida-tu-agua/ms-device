namespace SyWater.Devices.Domain.Devices;

/// <summary>What happened with one heartbeat. Only used for logs: the device never gets an answer.</summary>
public enum HeartbeatOutcome
{
    Accepted,
    UnknownDevice,
    InvalidToken,
    Revoked,
}
