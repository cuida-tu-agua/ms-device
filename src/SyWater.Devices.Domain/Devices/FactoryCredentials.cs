using System.Security.Cryptography;

namespace SyWater.Devices.Domain.Devices;

/// <summary>What a new device gets at the factory. The plain values exist ONLY here, once: the database keeps hashes.</summary>
public sealed record FactoryCredentials(string AuthToken, string PairingCode)
{
    /// <summary>The code as printed on the box: ABCD-EFGH.</summary>
    public string PairingCodeLabel => $"{PairingCode[..4]}-{PairingCode[4..]}";

    /// <summary>Same recipe as scripts/new-device.ps1: 24 random bytes → 32 URL-safe characters, and 8 characters without look-alikes.</summary>
    public static FactoryCredentials Generate()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var alphabet = DeviceSecrets.PairingAlphabet;
        var code = new string(Enumerable.Range(0, DeviceSecrets.PairingCodeLength)
            .Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
        return new FactoryCredentials(token, code);
    }
}

/// <summary>Serial numbers of the factory sequence: SW-ESP32-000001, SW-ESP32-000002…</summary>
public static class FactorySerials
{
    public const string Prefix = "SW-ESP32-";
    public const int MaxBatch = 50;

    /// <summary>The next <paramref name="count"/> serials after the last one registered (null = none yet).</summary>
    public static IReadOnlyList<string> Next(string? lastSerial, int count)
    {
        if (count is < 1 or > MaxBatch)
            throw new InvalidDeviceDataException($"You can register from 1 to {MaxBatch} devices at a time.");

        var last = 0;
        if (lastSerial is not null && lastSerial.StartsWith(Prefix, StringComparison.Ordinal)
            && int.TryParse(lastSerial[Prefix.Length..], out var n))
            last = n;

        return Enumerable.Range(last + 1, count).Select(i => $"{Prefix}{i:D6}").ToList();
    }
}
