using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SyWater.Devices.Domain.Devices;

/// <summary>
/// Format and hashing rules of the factory secrets. Pure functions (only the .NET base library).
/// The database stores SHA-256 hashes; the plain token lives in the firmware and
/// the plain pairing code is printed on the box.
/// </summary>
public static partial class DeviceSecrets
{
    /// <summary>Characters used by pairing codes: no 0/O and no 1/I/L, so they cannot be confused.</summary>
    public const string PairingAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int PairingCodeLength = 8;

    [GeneratedRegex("^[A-Z0-9-]{4,32}$")]
    private static partial Regex SerialFormat();

    /// <summary>" sw-esp32-000001 " → "SW-ESP32-000001". Throws if the format is not valid.</summary>
    public static string NormalizeSerial(string? serial)
    {
        var value = (serial ?? "").Trim().ToUpperInvariant();
        if (!SerialFormat().IsMatch(value))
            throw new InvalidDeviceDataException("The serial number must have 4 to 32 letters, digits or dashes.");
        return value;
    }

    /// <summary>"4hzx-vcdp" or "4HZX VCDP" → "4HZXVCDP". Throws if the format is not valid.</summary>
    public static string NormalizePairingCode(string? code)
    {
        var value = new string((code ?? "").Where(c => c != '-' && !char.IsWhiteSpace(c)).ToArray())
            .ToUpperInvariant();

        if (value.Length != PairingCodeLength || value.Any(c => !PairingAlphabet.Contains(c)))
            throw new InvalidDeviceDataException($"The pairing code must have {PairingCodeLength} characters (e.g. ABCD-EFGH).");
        return value;
    }

    /// <summary>SHA-256 of the token, as 64 lowercase hex chars (same as the SQL seed and new-device.ps1).</summary>
    public static string HashToken(string token) => Sha256Hex(token);

    /// <summary>The serial is part of the input, so two devices with the same code get different hashes.</summary>
    public static string HashPairingCode(string normalizedSerial, string normalizedCode) =>
        Sha256Hex($"{normalizedSerial}:{normalizedCode}");

    /// <summary>Constant-time comparison: the time taken does not reveal how many characters matched.</summary>
    public static bool HashesMatch(string expectedHex, string actualHex) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHex),
            Encoding.ASCII.GetBytes(actualHex));

    private static string Sha256Hex(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
