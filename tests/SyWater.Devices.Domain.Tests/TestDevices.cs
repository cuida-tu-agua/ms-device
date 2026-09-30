using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Domain.Tests;

/// <summary>Builds devices the way the factory seed does (hashes of known plain values).</summary>
internal static class TestDevices
{
    public const string Serial = "SW-ESP32-000001";
    public const string Token = "1gQn7MIHy0iGmO2hDpGM1n_uAdZRQUtJ";
    public const string Code = "4HZXVCDP";
    public static readonly DateTime Now = new(2026, 9, 28, 15, 0, 0, DateTimeKind.Utc);

    public static Device New(DateTime? lastReportAt = null, Guid? placeId = null, Guid? linkedBy = null,
        DateTime? revokedAt = null, int thresholdMinutes = 10) =>
        Device.Restore(
            id: Guid.NewGuid(),
            serialNumber: Serial,
            authTokenHash: DeviceSecrets.HashToken(Token),
            authTokenLastUsedAt: null,
            authTokenRevokedAt: revokedAt,
            pairingCodeHash: DeviceSecrets.HashPairingCode(Serial, Code),
            pairingFailedAttempts: 0,
            pairingLockedUntil: null,
            placeId: placeId,
            linkedBy: placeId is null ? null : linkedBy ?? Guid.NewGuid(),
            linkedAt: placeId is null ? null : Now.AddDays(-1),
            status: lastReportAt is null ? DeviceStatus.NeverReported : DeviceStatus.Connected,
            lastReportAt: lastReportAt,
            firmwareVersion: null,
            inactivityThresholdMinutes: thresholdMinutes,
            createdAt: Now.AddDays(-30),
            updatedAt: Now.AddDays(-30));
}
