namespace SyWater.Devices.Domain.Devices;

/// <summary>
/// One row of devices.device_link_history: the device was linked to a place from
/// <see cref="LinkedAt"/> until <see cref="UnlinkedAt"/> (null = still linked).
/// </summary>
public sealed class DeviceLink
{
    public Guid Id { get; }
    public Guid DeviceId { get; }
    public Guid PlaceId { get; }
    public Guid LinkedBy { get; }
    public DateTime LinkedAt { get; }
    public DateTime? UnlinkedAt { get; }
    public Guid? UnlinkedBy { get; }

    private DeviceLink(Guid id, Guid deviceId, Guid placeId, Guid linkedBy, DateTime linkedAt,
        DateTime? unlinkedAt, Guid? unlinkedBy)
    {
        Id = id;
        DeviceId = deviceId;
        PlaceId = placeId;
        LinkedBy = linkedBy;
        LinkedAt = linkedAt;
        UnlinkedAt = unlinkedAt;
        UnlinkedBy = unlinkedBy;
    }

    /// <summary>New open link (created by <see cref="Device.LinkTo"/>).</summary>
    internal static DeviceLink Open(Guid deviceId, Guid placeId, Guid userId, DateTime now) =>
        new(Guid.NewGuid(), deviceId, placeId, userId, now, null, null);

    /// <summary>Rebuilds a link read from the database.</summary>
    public static DeviceLink Restore(Guid id, Guid deviceId, Guid placeId, Guid linkedBy, DateTime linkedAt,
        DateTime? unlinkedAt, Guid? unlinkedBy) =>
        new(id, deviceId, placeId, linkedBy, linkedAt, unlinkedAt, unlinkedBy);

    public bool IsOpen => UnlinkedAt is null;
}
