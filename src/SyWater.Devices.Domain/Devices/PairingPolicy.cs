namespace SyWater.Devices.Domain.Devices;

/// <summary>
/// Brute-force protection for pairing codes: after <see cref="MaxFailedAttempts"/> wrong codes
/// on the same device, pairing is blocked for <see cref="LockDuration"/>.
/// (The API also limits how many link attempts each USER can make; see PairingRateLimiter.)
/// </summary>
public sealed record PairingPolicy(int MaxFailedAttempts, TimeSpan LockDuration)
{
    public static PairingPolicy Default { get; } = new(5, TimeSpan.FromMinutes(15));
}
