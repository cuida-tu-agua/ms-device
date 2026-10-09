using SyWater.Devices.Domain.Devices;
using SyWater.Devices.Infrastructure.Persistence.Entities;

namespace SyWater.Devices.Infrastructure.Persistence;

/// <summary>
/// Converts between the domain (enums, invariants) and the database (strings validated by CHECK constraints).
/// The string values must match CK_devices_status in ms-devices-db exactly.
/// </summary>
internal static class DeviceMapper
{
    public const string Connected = "CONNECTED";
    public const string Disconnected = "DISCONNECTED";
    public const string NeverReported = "NEVER_REPORTED";

    public static Device ToDomain(DeviceEntity e) => Device.Restore(
        id: e.Id,
        serialNumber: e.SerialNumber,
        authTokenHash: e.AuthTokenHash,
        authTokenLastUsedAt: AsUtc(e.AuthTokenLastUsedAt),
        authTokenRevokedAt: AsUtc(e.AuthTokenRevokedAt),
        pairingCodeHash: e.PairingCodeHash,
        pairingFailedAttempts: e.PairingFailedAttempts,
        pairingLockedUntil: AsUtc(e.PairingLockedUntil),
        placeId: e.PlaceId,
        linkedBy: e.LinkedBy,
        linkedAt: AsUtc(e.LinkedAt),
        status: ToStatus(e.Status),
        lastReportAt: AsUtc(e.LastReportAt),
        firmwareVersion: e.FirmwareVersion,
        inactivityThresholdMinutes: e.InactivityThresholdMin,
        createdAt: AsUtc(e.CreatedAt),
        updatedAt: AsUtc(e.UpdatedAt));

    public static DeviceEntity ToEntity(Device d) => new()
    {
        Id = d.Id,
        SerialNumber = d.SerialNumber,
        AuthTokenHash = d.AuthTokenHash,
        AuthTokenLastUsedAt = d.AuthTokenLastUsedAt,
        AuthTokenRevokedAt = d.AuthTokenRevokedAt,
        PairingCodeHash = d.PairingCodeHash,
        PairingFailedAttempts = d.PairingFailedAttempts,
        PairingLockedUntil = d.PairingLockedUntil,
        PlaceId = d.PlaceId,
        LinkedBy = d.LinkedBy,
        LinkedAt = d.LinkedAt,
        Status = ToDb(d.Status),
        LastReportAt = d.LastReportAt,
        FirmwareVersion = d.FirmwareVersion,
        InactivityThresholdMin = d.InactivityThresholdMinutes,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
    };

    public static DeviceLinkEntity ToEntity(DeviceLink link) => new()
    {
        Id = link.Id,
        DeviceId = link.DeviceId,
        PlaceId = link.PlaceId,
        LinkedBy = link.LinkedBy,
        LinkedAt = link.LinkedAt,
        UnlinkedAt = link.UnlinkedAt,
        UnlinkedBy = link.UnlinkedBy,
    };

    public static string ToDb(DeviceStatus status) => status switch
    {
        DeviceStatus.Connected => Connected,
        DeviceStatus.Disconnected => Disconnected,
        DeviceStatus.NeverReported => NeverReported,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static DeviceStatus ToStatus(string value) => value switch
    {
        Connected => DeviceStatus.Connected,
        Disconnected => DeviceStatus.Disconnected,
        NeverReported => DeviceStatus.NeverReported,
        _ => throw new InvalidOperationException($"Unknown device status '{value}' in the database."),
    };

    /// <summary>SQL Server DATETIME2 has no time zone; every date in sy-water-db is UTC (SYSUTCDATETIME).</summary>
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? AsUtc(DateTime? value) => value is null ? null : AsUtc(value.Value);
}
