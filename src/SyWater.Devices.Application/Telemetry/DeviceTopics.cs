namespace SyWater.Devices.Application.Telemetry;

public static class DeviceTopics
{
    public const string Prefix = "sywater/devices/";

    public const string Heartbeat = "heartbeat";
    public const string Telemetry = "telemetry";
    public const string ValveState = "valve/state";
    public const string ValveSet = "valve/set";

    public static readonly string[] Subscriptions =
    [
        Prefix + "+/" + Heartbeat,
        Prefix + "+/" + Telemetry,
        Prefix + "+/" + ValveState,
    ];

    public static string ValveSetFor(string serialNumber) => Prefix + serialNumber + "/" + ValveSet;

    public static bool TryParse(string? topic, out string serial, out string kind)
    {
        serial = kind = "";
        if (topic is null || !topic.StartsWith(Prefix, StringComparison.Ordinal)) return false;

        var rest = topic[Prefix.Length..];
        var slash = rest.IndexOf('/');
        if (slash <= 0) return false;

        serial = rest[..slash].ToUpperInvariant();
        kind = rest[(slash + 1)..];
        return kind is Heartbeat or Telemetry or ValveState;
    }
}
