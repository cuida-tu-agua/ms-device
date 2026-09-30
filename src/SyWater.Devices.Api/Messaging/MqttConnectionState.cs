using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SyWater.Devices.Api.Messaging;

/// <summary>Shared flag: the listener writes it, /health reads it.</summary>
public sealed class MqttConnectionState
{
    private volatile bool _connected;
    public bool IsConnected { get => _connected; set => _connected = value; }
}

/// <summary>/health shows "Degraded" (still HTTP 200) while the broker is not reachable.</summary>
public sealed class MqttHealthCheck(MqttConnectionState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        Task.FromResult(state.IsConnected
            ? HealthCheckResult.Healthy("Connected to the MQTT broker.")
            : HealthCheckResult.Degraded("Not connected to the MQTT broker: heartbeats are not being received."));
}
