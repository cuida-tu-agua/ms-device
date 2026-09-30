using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SyWater.Devices.Application.Heartbeats;

/// <summary>
/// MQTT contract with the ESP32 firmware.
///   Topic:   sywater/devices/{serial}/heartbeat
///   Payload: {"token":"&lt;factory token&gt;","fw":"1.0.0"}
/// </summary>
public sealed record HeartbeatMessage(string SerialNumber, string Token, string? FirmwareVersion)
{
    public const string TopicPrefix = "sywater/devices/";
    public const string TopicSuffix = "/heartbeat";

    /// <summary>Subscription filter: "+" matches exactly one level (the serial).</summary>
    public const string TopicFilter = TopicPrefix + "+" + TopicSuffix;

    /// <summary>Largest payload we accept; anything bigger is ignored without parsing.</summary>
    public const int MaxPayloadLength = 512;

    /// <summary>
    /// Reads topic + payload. Returns false (never throws) for anything malformed:
    /// a bad message from one device must not stop the listener.
    /// </summary>
    public static bool TryParse(string? topic, string? payload, [NotNullWhen(true)] out HeartbeatMessage? message)
    {
        message = null;
        if (topic is null || payload is null || payload.Length > MaxPayloadLength) return false;
        if (!topic.StartsWith(TopicPrefix, StringComparison.Ordinal) ||
            !topic.EndsWith(TopicSuffix, StringComparison.Ordinal)) return false;

        var serial = topic[TopicPrefix.Length..^TopicSuffix.Length];
        if (serial.Length == 0 || serial.Contains('/')) return false;

        try
        {
            using var json = JsonDocument.Parse(payload);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;

            if (!root.TryGetProperty("token", out var tokenElement) || tokenElement.ValueKind != JsonValueKind.String)
                return false;
            var token = tokenElement.GetString();
            if (string.IsNullOrWhiteSpace(token)) return false;

            string? firmware = root.TryGetProperty("fw", out var fw) && fw.ValueKind == JsonValueKind.String
                ? fw.GetString()
                : null;

            message = new HeartbeatMessage(serial.ToUpperInvariant(), token, firmware);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
