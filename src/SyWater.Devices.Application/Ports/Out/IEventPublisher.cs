using SyWater.Devices.Application.Events;

namespace SyWater.Devices.Application.Ports.Out;

public interface IEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken ct);
}
