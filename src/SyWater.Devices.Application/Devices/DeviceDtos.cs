using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Devices;

/// <summary>HU-012 input. UserId comes from the token, never from the body.</summary>
public sealed record LinkDeviceCommand(Guid UserId, Guid PlaceId, string SerialNumber, string PairingCode);

/// <summary>What the app sees about a device (never the hashes).</summary>
public sealed record DeviceView(
    Guid Id,
    string SerialNumber,
    Guid PlaceId,
    DeviceStatus Status,
    DateTime? LastReportAt,
    DateTime LinkedAt,
    string? FirmwareVersion,
    int InactivityThresholdMinutes)
{
    /// <summary>Only for linked devices. Status is calculated "now", not read from the column.</summary>
    public static DeviceView From(Device device, DateTime now) => new(
        device.Id,
        device.SerialNumber,
        device.PlaceId ?? throw new InvalidOperationException("Only linked devices have a view."),
        device.StatusAt(now),
        device.LastReportAt,
        device.LinkedAt!.Value,
        device.FirmwareVersion,
        device.InactivityThresholdMinutes);
}

/// <summary>
/// Settings of the use cases (read from appsettings "Devices" in the composition root).
/// A plain class: Application has no NuGet packages, so it does not use IOptions.
/// </summary>
public sealed record DevicePolicy(bool RequireOnlineToLink, PairingPolicy Pairing)
{
    public static DevicePolicy Default { get; } = new(true, PairingPolicy.Default);
}
