using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Domain.Tests;

public class DeviceTests
{
    private static readonly DateTime Now = TestDevices.Now;
    private static readonly PairingPolicy Policy = new(3, TimeSpan.FromMinutes(15));

    // ── HU-013: status ───────────────────────────────────────────────────

    [Fact]
    public void StatusAt_is_NeverReported_without_reports() =>
        Assert.Equal(DeviceStatus.NeverReported, TestDevices.New().StatusAt(Now));

    [Fact]
    public void StatusAt_is_Connected_inside_the_threshold() =>
        Assert.Equal(DeviceStatus.Connected, TestDevices.New(lastReportAt: Now.AddMinutes(-10)).StatusAt(Now));

    [Fact]
    public void StatusAt_is_Disconnected_after_the_threshold() =>
        Assert.Equal(DeviceStatus.Disconnected,
            TestDevices.New(lastReportAt: Now.AddMinutes(-10).AddSeconds(-1)).StatusAt(Now));

    [Fact]
    public void StatusAt_uses_each_device_threshold() =>
        Assert.Equal(DeviceStatus.Connected,
            TestDevices.New(lastReportAt: Now.AddMinutes(-20), thresholdMinutes: 30).StatusAt(Now));

    // ── Heartbeats ───────────────────────────────────────────────────────

    [Fact]
    public void RecordHeartbeat_with_the_factory_token_connects_the_device()
    {
        var device = TestDevices.New();

        var outcome = device.RecordHeartbeat(TestDevices.Token, " 1.0.3 ", Now);

        Assert.Equal(HeartbeatOutcome.Accepted, outcome);
        Assert.Equal(DeviceStatus.Connected, device.Status);
        Assert.Equal(Now, device.LastReportAt);
        Assert.Equal(Now, device.AuthTokenLastUsedAt);
        Assert.Equal("1.0.3", device.FirmwareVersion);
    }

    [Fact]
    public void RecordHeartbeat_with_a_wrong_token_changes_nothing()
    {
        var device = TestDevices.New();

        Assert.Equal(HeartbeatOutcome.InvalidToken, device.RecordHeartbeat("not-the-token", null, Now));
        Assert.Null(device.LastReportAt);
        Assert.Equal(DeviceStatus.NeverReported, device.Status);
    }

    [Fact]
    public void RecordHeartbeat_of_a_revoked_device_is_rejected() =>
        Assert.Equal(HeartbeatOutcome.Revoked,
            TestDevices.New(revokedAt: Now.AddDays(-1)).RecordHeartbeat(TestDevices.Token, null, Now));

    [Fact]
    public void RecordHeartbeat_keeps_the_old_firmware_when_none_is_sent()
    {
        var device = TestDevices.New();
        device.RecordHeartbeat(TestDevices.Token, "1.0.0", Now);

        device.RecordHeartbeat(TestDevices.Token, null, Now.AddMinutes(1));

        Assert.Equal("1.0.0", device.FirmwareVersion);
    }

    // ── HU-012: pairing code ─────────────────────────────────────────────

    [Fact]
    public void PairingCodeMatches_accepts_the_code_on_the_box()
    {
        var device = TestDevices.New();
        Assert.True(device.PairingCodeMatches(TestDevices.Code));
        Assert.False(device.PairingCodeMatches("AAAAAAAA"));
    }

    [Fact]
    public void RegisterFailedPairing_counts_failed_attempts()
    {
        var device = TestDevices.New();

        device.RegisterFailedPairing(Now, Policy);

        Assert.Equal(1, device.PairingFailedAttempts);
        Assert.Null(device.PairingLockedUntil);
    }

    [Fact]
    public void RegisterFailedPairing_locks_at_the_limit_and_restarts_the_counter()
    {
        var device = TestDevices.New();
        for (var i = 0; i < Policy.MaxFailedAttempts; i++)
            device.RegisterFailedPairing(Now, Policy);

        Assert.Equal(Now.AddMinutes(15), device.PairingLockedUntil);
        Assert.Equal(0, device.PairingFailedAttempts);
    }

    [Fact]
    public void EnsurePairingNotLocked_throws_while_locked_even_for_the_right_code()
    {
        var device = TestDevices.New();
        for (var i = 0; i < Policy.MaxFailedAttempts; i++)
            device.RegisterFailedPairing(Now, Policy);

        var ex = Assert.Throws<PairingLockedException>(() => device.EnsurePairingNotLocked(Now.AddMinutes(5)));
        Assert.Equal(Now.AddMinutes(15), ex.LockedUntilUtc);
    }

    [Fact]
    public void EnsurePairingNotLocked_passes_when_the_lock_expires()
    {
        var device = TestDevices.New();
        for (var i = 0; i < Policy.MaxFailedAttempts; i++)
            device.RegisterFailedPairing(Now, Policy);

        device.EnsurePairingNotLocked(Now.AddMinutes(16)); // does not throw
    }

    // ── HU-012: link ─────────────────────────────────────────────────────

    [Fact]
    public void LinkTo_links_an_online_device_and_opens_a_history_row()
    {
        var device = TestDevices.New(lastReportAt: Now.AddSeconds(-30));
        var placeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        device.RegisterFailedPairing(Now, Policy); // one typo before the right code

        var link = device.LinkTo(placeId, userId, Now, requireOnline: true);

        Assert.Equal(0, device.PairingFailedAttempts); // a successful link clears the attempts
        Assert.Equal(placeId, device.PlaceId);
        Assert.True(device.IsLinkedBy(userId));
        Assert.Equal(Now, device.LinkedAt);
        Assert.Equal(device.Id, link.DeviceId);
        Assert.Equal(placeId, link.PlaceId);
        Assert.True(link.IsOpen);
    }

    [Fact]
    public void LinkTo_rejects_a_device_that_is_already_linked() =>
        Assert.Throws<DeviceAlreadyLinkedException>(() =>
            TestDevices.New(lastReportAt: Now, placeId: Guid.NewGuid())
                .LinkTo(Guid.NewGuid(), Guid.NewGuid(), Now, requireOnline: true));

    [Fact]
    public void LinkTo_rejects_an_offline_device_when_online_is_required() =>
        Assert.Throws<DeviceOfflineException>(() =>
            TestDevices.New(lastReportAt: Now.AddHours(-1)).LinkTo(Guid.NewGuid(), Guid.NewGuid(), Now, requireOnline: true));

    [Fact]
    public void LinkTo_allows_a_device_that_never_reported_when_online_is_not_required() =>
        Assert.True(TestDevices.New().LinkTo(Guid.NewGuid(), Guid.NewGuid(), Now, requireOnline: false).IsOpen);

    [Fact]
    public void LinkTo_rejects_a_revoked_device() =>
        Assert.Throws<DeviceRevokedException>(() =>
            TestDevices.New(lastReportAt: Now, revokedAt: Now.AddDays(-1))
                .LinkTo(Guid.NewGuid(), Guid.NewGuid(), Now, requireOnline: false));

    // ── HU-014: unlink ───────────────────────────────────────────────────

    [Fact]
    public void Unlink_clears_the_link_and_returns_the_old_place()
    {
        var placeId = Guid.NewGuid();
        var device = TestDevices.New(lastReportAt: Now, placeId: placeId);

        Assert.Equal(placeId, device.Unlink(Now));
        Assert.False(device.IsLinked);
        Assert.Null(device.LinkedBy);
        Assert.Null(device.LinkedAt);
    }

    [Fact]
    public void Unlink_of_a_free_device_fails() =>
        Assert.Throws<DeviceNotLinkedException>(() => TestDevices.New().Unlink(Now));
}
