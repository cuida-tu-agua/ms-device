using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SyWater.Devices.Application.Telemetry;

public sealed record TelemetryMessage(
    string SerialNumber, string Token, DateTime RecordedAt, decimal FlowLpm, decimal VolumeLiters, decimal TotalLiters)
{
    public const int MaxPayloadLength = 512;

    public static bool TryParse(string serial, string? payload, [NotNullWhen(true)] out TelemetryMessage? message)
    {
        message = null;
        if (payload is null || payload.Length > MaxPayloadLength) return false;

        try
        {
            using var json = JsonDocument.Parse(payload);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;

            if (!root.TryGetProperty("token", out var token) || token.ValueKind != JsonValueKind.String) return false;
            if (string.IsNullOrWhiteSpace(token.GetString())) return false;
            if (!TryNumber(root, "ts", out var ts) || !TryNumber(root, "lpm", out var lpm) ||
                !TryNumber(root, "vol", out var vol) || !TryNumber(root, "total", out var total)) return false;
            if (ts is < 0 or > 253402300799m) return false; // year 9999

            var recordedAt = DateTimeOffset.FromUnixTimeSeconds((long)ts).UtcDateTime;
            message = new TelemetryMessage(serial, token.GetString()!, recordedAt, lpm, vol, total);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryNumber(JsonElement root, string name, out decimal value)
    {
        value = 0;
        return root.TryGetProperty(name, out var element)
               && element.ValueKind == JsonValueKind.Number
               && element.TryGetDecimal(out value);
    }
}

public static class TelemetryRules
{
    public const decimal MaxFlowLpm = 60m;
    public const decimal MaxVolumePerReading = 100m;   // 10 s at 60 L/min = 10 L; generous for a late interval
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);       // the ESP32 buffers 1 h when offline
    public static readonly TimeSpan MaxClockAhead = TimeSpan.FromMinutes(5);

    public static string? Validate(TelemetryMessage m, DateTime now)
    {
        if (m.FlowLpm < 0 || m.VolumeLiters < 0 || m.TotalLiters < 0) return "negative value";
        if (m.FlowLpm > MaxFlowLpm) return $"flow {m.FlowLpm} L/min above {MaxFlowLpm}";
        if (m.VolumeLiters > MaxVolumePerReading) return $"volume {m.VolumeLiters} L above {MaxVolumePerReading}";
        if (m.RecordedAt > now + MaxClockAhead) return "timestamp in the future (device clock not synced?)";
        if (m.RecordedAt < now - MaxAge) return "timestamp older than 24 h";
        return null;
    }
}
