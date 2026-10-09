using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Application.UseCases;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Tests;

/// <summary>In-memory adapter of the admin repository: same rules as the SQL one (unique serial, log row with each change).</summary>
internal sealed class FakeAdminDeviceRepository : IAdminDeviceRepository
{
    public List<Device> Devices { get; } = [];
    public List<(Guid DeviceId, AdminLogEntry Entry)> Log { get; } = [];

    public Task<AdminDevicePage> ListAsync(AdminDeviceQuery query, DateTime now, CancellationToken ct)
    {
        var rows = Devices.Select(d => new AdminDeviceRow(d.Id, d.SerialNumber, d.StatusAt(now), d.IsRevoked, d.IsLinked, d.LastReportAt, d.FirmwareVersion)).ToList();
        var counts = new AdminDeviceCounts(rows.Count, rows.Count(r => r.Linked),
            rows.Count(r => !r.Decommissioned && r.Status == DeviceStatus.Connected),
            rows.Count(r => !r.Decommissioned && r.Status == DeviceStatus.Disconnected),
            rows.Count(r => !r.Decommissioned && r.Status == DeviceStatus.NeverReported),
            rows.Count(r => r.Decommissioned));
        return Task.FromResult(new AdminDevicePage(rows, query.Page, query.Size, rows.Count, counts));
    }

    public Task<AdminDeviceDetail?> GetDetailAsync(Guid id, DateTime now, CancellationToken ct)
    {
        var d = Devices.FirstOrDefault(x => x.Id == id);
        if (d is null) return Task.FromResult<AdminDeviceDetail?>(null);
        var log = Log.Where(l => l.DeviceId == id).Select(l => l.Entry).OrderByDescending(l => l.OccurredAt).ToList();
        return Task.FromResult<AdminDeviceDetail?>(new AdminDeviceDetail(d.Id, d.SerialNumber, d.StatusAt(now), d.IsRevoked, d.AuthTokenRevokedAt,
            d.PlaceId, d.LinkedAt, d.LastReportAt, d.FirmwareVersion, d.CreatedAt, d.AuthTokenLastUsedAt, d.PairingFailedAttempts, [], log));
    }

    public Task<Device?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Devices.FirstOrDefault(d => d.Id == id));

    public Task<string?> GetLastFactorySerialAsync(CancellationToken ct) =>
        Task.FromResult(Devices.Select(d => d.SerialNumber).Where(s => s.StartsWith(FactorySerials.Prefix)).OrderDescending().FirstOrDefault());

    public Task AddAsync(IReadOnlyList<Device> devices, AdminLogEntry log, CancellationToken ct)
    {
        var taken = devices.FirstOrDefault(n => Devices.Any(d => d.SerialNumber == n.SerialNumber));
        if (taken is not null) throw new SerialAlreadyExistsException(taken.SerialNumber);
        foreach (var device in devices)
        {
            Devices.Add(device);
            Log.Add((device.Id, log));
        }
        return Task.CompletedTask;
    }

    public Task SaveCredentialsAsync(Device device, AdminLogEntry log, CancellationToken ct)
    {
        Log.Add((device.Id, log));
        return Task.CompletedTask;
    }

    public Task SaveDecommissionAsync(Device device, AdminLogEntry log, CancellationToken ct)
    {
        Log.Add((device.Id, log));
        return Task.CompletedTask;
    }
}

public class AdminDeviceUseCaseTests
{
    private static readonly DateTime Start = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);
    private readonly FakeClock _clock = new(Start);
    private readonly FakeAdminDeviceRepository _repo = new();
    private readonly AdminActor _admin = new(Guid.NewGuid(), "Ana Admin");

    private RegisterDevicesUseCase Register() => new(_repo, _clock);

    [Fact]
    public async Task Registering_three_creates_the_next_serials_and_returns_the_plain_credentials_once()
    {
        var created = await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(3, null), default);

        Assert.Equal(["SW-ESP32-000001", "SW-ESP32-000002", "SW-ESP32-000003"], created.Select(c => c.SerialNumber));
        Assert.Equal(3, _repo.Devices.Count);
        // what is shown is what the hash of the stored device matches
        var first = created[0];
        var stored = _repo.Devices.Single(d => d.Id == first.Id);
        Assert.Equal(DeviceSecrets.HashToken(first.AuthToken), stored.AuthTokenHash);
        Assert.True(stored.PairingCodeMatches(DeviceSecrets.NormalizePairingCode(first.PairingCode)));
        Assert.DoesNotContain(first.AuthToken, stored.AuthTokenHash);
    }

    [Fact]
    public async Task The_sequence_continues_where_it_stopped()
    {
        await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(2, null), default);

        var next = await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(1, null), default);

        Assert.Equal("SW-ESP32-000003", Assert.Single(next).SerialNumber);
    }

    [Fact]
    public async Task One_specific_serial_is_registered_and_a_repeated_one_is_refused()
    {
        var one = await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(null, " sw-esp32-000100 "), default);
        Assert.Equal("SW-ESP32-000100", Assert.Single(one).SerialNumber);

        var again = await Assert.ThrowsAsync<SerialAlreadyExistsException>(() =>
            Register().ExecuteAsync(_admin, new RegisterDevicesCommand(null, "SW-ESP32-000100"), default));
        Assert.Equal("device.serial_exists", again.Code);
        Assert.Single(_repo.Devices);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task The_batch_is_from_one_to_fifty(int count)
    {
        await Assert.ThrowsAsync<InvalidDeviceDataException>(() => Register().ExecuteAsync(_admin, new RegisterDevicesCommand(count, null), default));
        Assert.Empty(_repo.Devices);
    }

    [Fact]
    public async Task Registering_leaves_a_log_row_with_the_administrator()
    {
        var created = await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(1, null), default);

        var entry = Assert.Single(_repo.Log, l => l.DeviceId == created[0].Id).Entry;
        Assert.Equal(AdminAction.Registered, entry.Action);
        Assert.Equal(_admin.UserId, entry.AdminUserId);
        Assert.Equal("Ana Admin", entry.AdminName);
        Assert.Equal(Start, entry.OccurredAt);
    }

    [Fact]
    public async Task New_credentials_replace_the_old_ones_and_are_logged()
    {
        var created = (await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(1, null), default))[0];
        _clock.UtcNow = Start.AddDays(1);

        var renewed = await new RegenerateCredentialsUseCase(_repo, _clock).ExecuteAsync(_admin, created.Id, default);

        Assert.NotEqual(created.AuthToken, renewed.AuthToken);
        var stored = _repo.Devices.Single();
        Assert.Equal(DeviceSecrets.HashToken(renewed.AuthToken), stored.AuthTokenHash);
        Assert.Contains(_repo.Log, l => l.Entry.Action == AdminAction.CredentialsRegenerated);
    }

    [Fact]
    public async Task New_credentials_for_a_linked_or_unknown_device_are_refused_and_nothing_is_logged()
    {
        var created = (await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(1, null), default))[0];
        _repo.Devices.Single().LinkTo(Guid.NewGuid(), Guid.NewGuid(), Start, requireOnline: false);
        var logged = _repo.Log.Count;

        await Assert.ThrowsAsync<DeviceStillLinkedException>(() => new RegenerateCredentialsUseCase(_repo, _clock).ExecuteAsync(_admin, created.Id, default));
        await Assert.ThrowsAsync<DeviceNotFoundException>(() => new RegenerateCredentialsUseCase(_repo, _clock).ExecuteAsync(_admin, Guid.NewGuid(), default));
        Assert.Equal(logged, _repo.Log.Count);
    }

    [Fact]
    public async Task Decommissioning_returns_the_detail_with_the_new_state_and_the_log()
    {
        var created = (await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(1, null), default))[0];
        _clock.UtcNow = Start.AddDays(2);

        var detail = await new DecommissionDeviceUseCase(_repo, _clock).ExecuteAsync(_admin, created.Id, default);

        Assert.True(detail.Decommissioned);
        Assert.Equal(Start.AddDays(2), detail.DecommissionedAt);
        Assert.Equal([AdminAction.Decommissioned, AdminAction.Registered], detail.Log.Select(l => l.Action));
        await Assert.ThrowsAsync<DeviceAlreadyDecommissionedException>(() => new DecommissionDeviceUseCase(_repo, _clock).ExecuteAsync(_admin, created.Id, default));
    }

    [Fact]
    public async Task The_list_counts_decommissioned_devices_apart()
    {
        var created = await Register().ExecuteAsync(_admin, new RegisterDevicesCommand(2, null), default);
        await new DecommissionDeviceUseCase(_repo, _clock).ExecuteAsync(_admin, created[0].Id, default);

        var page = await new ListAdminDevicesUseCase(_repo, _clock).ExecuteAsync(AdminDeviceQuery.Normalize(null, AdminStatusFilter.All, AdminLinkFilter.All, null, null), default);

        Assert.Equal(2, page.Counts.Registered);
        Assert.Equal(1, page.Counts.Decommissioned);
        Assert.Equal(1, page.Counts.NeverReported);
        Assert.Equal(0, page.Counts.Connected);
    }

    [Fact]
    public async Task The_detail_of_an_unknown_device_is_not_found()
    {
        await Assert.ThrowsAsync<DeviceNotFoundException>(() => new GetAdminDeviceUseCase(_repo, _clock).ExecuteAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public void The_query_is_normalized_search_in_capitals_and_page_size_clamped()
    {
        var q = AdminDeviceQuery.Normalize("  sw-esp32-0000 ", AdminStatusFilter.All, AdminLinkFilter.All, -4, 5000);

        Assert.Equal("SW-ESP32-0000", q.Search);
        Assert.Equal(0, q.Page);
        Assert.Equal(AdminDeviceQuery.MaxSize, q.Size);
        Assert.Null(AdminDeviceQuery.Normalize("   ", AdminStatusFilter.All, AdminLinkFilter.All, null, null).Search);
    }
}
