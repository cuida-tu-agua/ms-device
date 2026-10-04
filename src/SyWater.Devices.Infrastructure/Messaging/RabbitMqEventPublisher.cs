using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SyWater.Devices.Application.Events;
using SyWater.Devices.Application.Ports.Out;

namespace SyWater.Devices.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(
    IOptions<RabbitMqOptions> options,
    TimeProvider clock,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1); // one publish at a time on the channel
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken ct)
    {
        var (id, body) = EventEnvelope.Serialize(integrationEvent, clock.GetUtcNow().UtcDateTime);
        var properties = new BasicProperties
        {
            MessageId = id.ToString(),
            Type = integrationEvent.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent, // survives a RabbitMQ restart
        };

        await _lock.WaitAsync(ct);
        try
        {
            var channel = await GetChannelAsync(ct);
            await channel.BasicPublishAsync(_options.Exchange, integrationEvent.Type, mandatory: false, properties, body, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Event {Type} {Id} could not be published to RabbitMQ and was lost", integrationEvent.Type, id);
            await ResetAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;
        await ResetAsync();

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            ClientProvidedName = "ms-devices",
        };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);

        await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false,
            cancellationToken: ct);
        logger.LogInformation("Connected to RabbitMQ {Host}:{Port}, exchange {Exchange}", _options.Host, _options.Port, _options.Exchange);
        return _channel;
    }

    private async Task ResetAsync()
    {
        try { if (_channel is not null) await _channel.DisposeAsync(); } catch { /* already broken */ }
        try { if (_connection is not null) await _connection.DisposeAsync(); } catch { /* already broken */ }
        _channel = null;
        _connection = null;
    }

    public async ValueTask DisposeAsync() => await ResetAsync();
}

public sealed class LogOnlyEventPublisher(ILogger<LogOnlyEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken ct)
    {
        logger.LogDebug("RabbitMQ disabled: event {Type} not published ({Event})", integrationEvent.Type, integrationEvent);
        return Task.CompletedTask;
    }
}
