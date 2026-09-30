namespace SyWater.Devices.Api.Messaging;

/// <summary>Section "Mqtt" of appsettings. The password goes in user-secrets, never in git.</summary>
public sealed class MqttOptions
{
    public const string Section = "Mqtt";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1883;
    public string Username { get; init; } = "ms-devices";
    public string Password { get; init; } = "";

    /// <summary>Must be unique per broker; two services with the same id kick each other out.</summary>
    public string ClientId { get; init; } = "ms-devices";

    /// <summary>Seconds between connection checks (and reconnection attempts).</summary>
    public int ReconnectSeconds { get; init; } = 5;
}
