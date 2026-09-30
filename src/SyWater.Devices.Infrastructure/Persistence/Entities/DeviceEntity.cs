using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyWater.Devices.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence model: a 1:1 copy of devices.devices (created by ms-devices-db).
/// It is NOT the domain entity; DeviceMapper converts between the two.
/// </summary>
[Table("devices", Schema = "devices")]
public sealed class DeviceEntity
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("serial_number")] public string SerialNumber { get; set; } = "";
    [Column("auth_token_hash")] public string AuthTokenHash { get; set; } = "";
    [Column("auth_token_last_used_at")] public DateTime? AuthTokenLastUsedAt { get; set; }
    [Column("auth_token_revoked_at")] public DateTime? AuthTokenRevokedAt { get; set; }
    [Column("pairing_code_hash")] public string PairingCodeHash { get; set; } = "";
    [Column("pairing_failed_attempts")] public int PairingFailedAttempts { get; set; }
    [Column("pairing_locked_until")] public DateTime? PairingLockedUntil { get; set; }
    [Column("place_id")] public Guid? PlaceId { get; set; }
    [Column("linked_by")] public Guid? LinkedBy { get; set; }
    [Column("linked_at")] public DateTime? LinkedAt { get; set; }
    [Column("status")] public string Status { get; set; } = "";
    [Column("last_report_at")] public DateTime? LastReportAt { get; set; }
    [Column("firmware_version")] public string? FirmwareVersion { get; set; }
    [Column("inactivity_threshold_min")] public int InactivityThresholdMin { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
