using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyWater.Devices.Infrastructure.Persistence.Entities;

/// <summary>Persistence model: a 1:1 copy of devices.device_admin_log (release v1.1 of ms-device-db).</summary>
[Table("device_admin_log", Schema = "devices")]
public sealed class DeviceAdminLogEntity
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("device_id")] public Guid DeviceId { get; set; }
    [Column("action")] public string Action { get; set; } = "";
    [Column("admin_user_id")] public Guid AdminUserId { get; set; }
    [Column("admin_name")] public string? AdminName { get; set; }
    [Column("occurred_at")] public DateTime OccurredAt { get; set; }
}
