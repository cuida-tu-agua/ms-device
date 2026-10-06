using System.Text.Json;
using SyWater.Devices.Application.Events;

namespace SyWater.Devices.Infrastructure.Messaging;

public static class EventEnvelope
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static (Guid Id, byte[] Body) Serialize(IIntegrationEvent integrationEvent, DateTime occurredAt)
    {
        var id = Guid.NewGuid();
        var envelope = new
        {
            id,
            type = integrationEvent.Type,
            occurredAt,
            data = (object)integrationEvent, // runtime type → all the properties of the record
        };
        return (id, JsonSerializer.SerializeToUtf8Bytes(envelope, Json));
    }
}
