using Microsoft.Extensions.Options;
using MQTTnet;
using SyWater.Devices.Application.Heartbeats;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Api.Messaging;

/// <summary>
/// Inbound adapter (like a controller, but for MQTT): subscribes to
/// "sywater/devices/+/heartbeat" in Mosquitto and hands every message to IRecordHeartbeatUseCase.
/// Runs for the whole life of the app and reconnects by itself if the broker goes down.
/// </summary>
public sealed class MqttHeartbeatListener(
    IServiceScopeFactory scopes,
    IOptions<MqttOptions> options,
    MqttConnectionState state,
    ILogger<MqttHeartbeatListener> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        // Attach the handler BEFORE connecting, otherwise queued messages would be lost.
        client.ApplicationMessageReceivedAsync += e => HandleAsync(e, stoppingToken);
        client.DisconnectedAsync += _ =>
        {
            state.IsConnected = false;
            return Task.CompletedTask;
        };

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(settings.Host, settings.Port)
            .WithCredentials(settings.Username, settings.Password)
            .WithClientId(settings.ClientId)
            .WithCleanSession()
            .Build();

        var subscription = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(HeartbeatMessage.TopicFilter).WithAtLeastOnceQoS())
            .Build();

        // Official MQTTnet v5 pattern: every few seconds, ping; if the ping fails, (re)connect.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await client.TryPingAsync(stoppingToken))
                {
                    await client.ConnectAsync(clientOptions, stoppingToken);
                    await client.SubscribeAsync(subscription, stoppingToken);
                    state.IsConnected = true;
                    logger.LogInformation("Connected to MQTT {Host}:{Port}, listening on {Topic}",
                        settings.Host, settings.Port, HeartbeatMessage.TopicFilter);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                state.IsConnected = false;
                logger.LogWarning("MQTT broker {Host}:{Port} not reachable ({Error}). Retrying in {Seconds}s",
                    settings.Host, settings.Port, ex.Message, settings.ReconnectSeconds);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.ReconnectSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        state.IsConnected = false;
        await client.TryDisconnectAsync();
    }

    private async Task HandleAsync(MqttApplicationMessageReceivedEventArgs e, CancellationToken ct)
    {
        var topic = e.ApplicationMessage.Topic;
        try
        {
            if (!HeartbeatMessage.TryParse(topic, e.ApplicationMessage.ConvertPayloadToString(), out var message))
            {
                logger.LogWarning("Ignored malformed heartbeat on {Topic}", topic);
                return;
            }

            // BackgroundService is a singleton; the use case and the DbContext are scoped (one per message).
            using var scope = scopes.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IRecordHeartbeatUseCase>();
            var outcome = await useCase.ExecuteAsync(message, ct);

            if (outcome == HeartbeatOutcome.Accepted)
                logger.LogDebug("Heartbeat from {Serial}", message.SerialNumber);
            else
                logger.LogWarning("Heartbeat from {Serial} rejected: {Outcome}", message.SerialNumber, outcome);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Never let one bad message (or a DB hiccup) kill the listener.
            logger.LogError(ex, "Error processing heartbeat on {Topic}", topic);
        }
    }
}
