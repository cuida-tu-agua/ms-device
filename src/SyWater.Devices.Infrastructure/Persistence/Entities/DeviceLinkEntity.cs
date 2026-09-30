using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyWater.Devices.Infrastructure.Persistence.Entities;

/// <summary>Persistence model: a 1:1 copy of devices.device_link_history.</summary>
[Table("device_link_history", Schema = "devices")]
public sealed class DeviceLinkEntity
{
    [Key, Column("id")] public Guid Id { get; set; }
    [Column("device_id")] public Guid DeviceId { get; set; }
    [Column("place_id")] public Guid PlaceId { get; set; }
    [Column("linked_by")] public Guid LinkedBy { get; set; }
    [Column("linked_at")] public DateTime LinkedAt { get; set; }
    [Column("unlinked_at")] public DateTime? UnlinkedAt { get; set; }
    [Column("unlinked_by")] public Guid? UnlinkedBy { get; set; }
    [Column("created_at"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public DateTime CreatedAt { get; set; }
}
