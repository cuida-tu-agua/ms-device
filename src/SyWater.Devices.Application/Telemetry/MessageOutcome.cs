using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Telemetry;

public sealed record MessageOutcome(string Code, string? Detail = null)
{
    public static readonly MessageOutcome Accepted = new("Accepted");
    public static readonly MessageOutcome UnknownDevice = new("UnknownDevice");
    public static readonly MessageOutcome InvalidToken = new("InvalidToken");
    public static readonly MessageOutcome Revoked = new("Revoked");
    public static readonly MessageOutcome NotLinked = new("NotLinked");

    public static MessageOutcome Rejected(string reason) => new("Rejected", reason);

    public bool IsAccepted => Code == "Accepted";

    public static MessageOutcome From(HeartbeatOutcome outcome) => outcome switch
    {
        HeartbeatOutcome.Accepted => Accepted,
        HeartbeatOutcome.UnknownDevice => UnknownDevice,
        HeartbeatOutcome.InvalidToken => InvalidToken,
        _ => Revoked,
    };

    public override string ToString() => Detail is null ? Code : $"{Code} ({Detail})";
}
