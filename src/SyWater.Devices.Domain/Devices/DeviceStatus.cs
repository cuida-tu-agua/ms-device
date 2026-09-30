namespace SyWater.Devices.Domain.Devices;

/// <summary>HU-013: connection status shown to the user. Stored as CONNECTED / DISCONNECTED / NEVER_REPORTED.</summary>
public enum DeviceStatus
{
    Connected,
    Disconnected,
    NeverReported,
}
