using Microsoft.Extensions.Diagnostics.HealthChecks;
using MQTTnet;

namespace SyWater.Devices.Api.Messaging;

public sealed class MqttConnectionState
{
    private volatile bool _connected;
    private volatile IMqttClient? _client;

    public bool IsConnected { get => _connected; set => _connected = value; }

    public IMqttClient? Client { get => _connected ? _client : null; set => _client = value; }
}

public sealed class MqttHealthCheck(MqttConnectionState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        Task.FromResult(state.IsConnected
            ? HealthCheckResult.Healthy("Connected to the MQTT broker.")
            : HealthCheckResult.Degraded("Not connected to the MQTT broker: heartbeats are not being received."));
}
