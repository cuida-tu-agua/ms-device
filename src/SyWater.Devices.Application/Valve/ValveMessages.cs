using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SyWater.Devices.Application.Valve;

public enum ValveAction { Open, Close }

public sealed record ValveStateMessage(string SerialNumber, string Token, string State, Guid? CommandId, DateTime? ReportedAt)
{
    public const int MaxPayloadLength = 512;

    public static bool TryParse(string serial, string? payload, [NotNullWhen(true)] out ValveStateMessage? message)
    {
        message = null;
        if (payload is null || payload.Length > MaxPayloadLength) return false;

        try
        {
            using var json = JsonDocument.Parse(payload);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;

            if (!root.TryGetProperty("token", out var token) || token.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(token.GetString())) return false;

            if (!root.TryGetProperty("state", out var stateElement) || stateElement.ValueKind != JsonValueKind.String)
                return false;
            var state = stateElement.GetString()!.Trim().ToUpperInvariant();
            if (state is not ("OPEN" or "CLOSED")) return false;

            Guid? commandId = null;
            if (root.TryGetProperty("commandId", out var cmd) && cmd.ValueKind == JsonValueKind.String
                && Guid.TryParse(cmd.GetString(), out var parsed)) commandId = parsed;

            DateTime? reportedAt = null;
            if (root.TryGetProperty("ts", out var ts) && ts.ValueKind == JsonValueKind.Number
                && ts.TryGetInt64(out var seconds) && seconds is > 0 and < 253402300799)
                reportedAt = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

            message = new ValveStateMessage(serial, token.GetString()!, state, commandId, reportedAt);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public static class ValveCommandPayload
{
    public static string Build(Guid commandId, ValveAction action) =>
        JsonSerializer.Serialize(new { commandId, action = action == ValveAction.Close ? "CLOSE" : "OPEN" });
}

public sealed record ValveCommandSent(Guid CommandId, string SerialNumber, DateTime SentAt);
