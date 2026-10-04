using Microsoft.Extensions.Options;
using MQTTnet;
using SyWater.Devices.Application.Heartbeats;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Telemetry;
using SyWater.Devices.Application.Valve;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Api.Messaging;

/// <summary>
/// Inbound adapter (like a controller, but for MQTT): subscribes to the three topics the ESP32
/// publishes (heartbeat, telemetry, valve/state) and hands every message to its use case.
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
        state.Client = client;

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

        var subscription = factory.CreateSubscribeOptionsBuilder();
        foreach (var topic in DeviceTopics.Subscriptions)
            subscription.WithTopicFilter(f => f.WithTopic(topic).WithAtLeastOnceQoS());
        var subscribeOptions = subscription.Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await client.TryPingAsync(stoppingToken))
                {
                    await client.ConnectAsync(clientOptions, stoppingToken);
                    await client.SubscribeAsync(subscribeOptions, stoppingToken);
                    state.IsConnected = true;
                    logger.LogInformation("Connected to MQTT {Host}:{Port}, listening on {Topics}",
                        settings.Host, settings.Port, string.Join(", ", DeviceTopics.Subscriptions));
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
        state.Client = null;
        await client.TryDisconnectAsync();
    }

    private async Task HandleAsync(MqttApplicationMessageReceivedEventArgs e, CancellationToken ct)
    {
        var topic = e.ApplicationMessage.Topic;
        try
        {
            if (!DeviceTopics.TryParse(topic, out var serial, out var kind))
            {
                logger.LogWarning("Ignored message on unexpected topic {Topic}", topic);
                return;
            }

            var payload = e.ApplicationMessage.ConvertPayloadToString();

            using var scope = scopes.CreateScope();
            var services = scope.ServiceProvider;

            switch (kind)
            {
                case DeviceTopics.Heartbeat:
                    await HandleHeartbeatAsync(services, topic, payload, ct);
                    break;
                case DeviceTopics.Telemetry:
                    if (!TelemetryMessage.TryParse(serial, payload, out var reading))
                    {
                        logger.LogWarning("Ignored malformed telemetry on {Topic}", topic);
                        return;
                    }
                    Log(serial, "Telemetry", await services.GetRequiredService<IRecordTelemetryUseCase>().ExecuteAsync(reading, ct));
                    break;
                case DeviceTopics.ValveState:
                    if (!ValveStateMessage.TryParse(serial, payload, out var valve))
                    {
                        logger.LogWarning("Ignored malformed valve state on {Topic}", topic);
                        return;
                    }
                    Log(serial, "Valve state " + valve.State, await services.GetRequiredService<IRecordValveStateUseCase>().ExecuteAsync(valve, ct));
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error processing message on {Topic}", topic);
        }
    }

    private async Task HandleHeartbeatAsync(IServiceProvider services, string topic, string payload, CancellationToken ct)
    {
        if (!HeartbeatMessage.TryParse(topic, payload, out var message))
        {
            logger.LogWarning("Ignored malformed heartbeat on {Topic}", topic);
            return;
        }

        var outcome = await services.GetRequiredService<IRecordHeartbeatUseCase>().ExecuteAsync(message, ct);
        if (outcome == HeartbeatOutcome.Accepted)
            logger.LogDebug("Heartbeat from {Serial}", message.SerialNumber);
        else
            logger.LogWarning("Heartbeat from {Serial} rejected: {Outcome}", message.SerialNumber, outcome);
    }

    private void Log(string serial, string what, MessageOutcome outcome)
    {
        if (outcome.IsAccepted)
            logger.LogDebug("{What} from {Serial}", what, serial);
        else if (outcome == MessageOutcome.NotLinked)
            logger.LogDebug("{What} from {Serial} ignored: the device is not linked to a place", what, serial);
        else
            logger.LogWarning("{What} from {Serial} rejected: {Outcome}", what, serial, outcome);
    }
}
