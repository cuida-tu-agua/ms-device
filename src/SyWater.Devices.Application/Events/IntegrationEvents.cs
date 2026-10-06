namespace SyWater.Devices.Application.Events;

public interface IIntegrationEvent
{
    string Type { get; }
}

public sealed record DeviceLinked(Guid DeviceId, string SerialNumber, Guid PlaceId, Guid UserId, DateTime LinkedAt)
    : IIntegrationEvent
{
    public const string EventType = "device.linked";
    public string Type => EventType;
}

public sealed record DeviceUnlinked(Guid DeviceId, string SerialNumber, Guid PlaceId, Guid UserId, DateTime UnlinkedAt)
    : IIntegrationEvent
{
    public const string EventType = "device.unlinked";
    public string Type => EventType;
}

public sealed record ReadingReceived(
    Guid DeviceId,
    Guid PlaceId,
    DateTime RecordedAt,
    decimal FlowLpm,
    decimal VolumeLiters,
    decimal TotalLiters,
    DateTime ReceivedAt) : IIntegrationEvent
{
    public const string EventType = "device.reading.received";
    public string Type => EventType;
}

public sealed record ValveReported(Guid DeviceId, Guid PlaceId, string State, Guid? CommandId, DateTime ReportedAt)
    : IIntegrationEvent
{
    public const string EventType = "device.valve.reported";
    public string Type => EventType;
}
