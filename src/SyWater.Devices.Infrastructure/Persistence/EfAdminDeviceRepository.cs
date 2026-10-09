using Microsoft.EntityFrameworkCore;
using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;
using SyWater.Devices.Infrastructure.Persistence.Entities;

namespace SyWater.Devices.Infrastructure.Persistence;

public sealed class EfAdminDeviceRepository(DevicesDbContext db) : IAdminDeviceRepository
{
    public async Task<AdminDevicePage> ListAsync(AdminDeviceQuery query, DateTime now, CancellationToken ct)
    {
        var all = db.Devices.AsNoTracking();
        var found = all;

        if (query.Search is not null) found = found.Where(d => d.SerialNumber.Contains(query.Search));
        found = query.Link switch
        {
            AdminLinkFilter.Linked => found.Where(d => d.PlaceId != null),
            AdminLinkFilter.Free => found.Where(d => d.PlaceId == null),
            _ => found,
        };
        found = query.Status switch
        {
            AdminStatusFilter.Connected => found.Where(d => d.AuthTokenRevokedAt == null && d.LastReportAt != null && d.LastReportAt >= now.AddMinutes(-d.InactivityThresholdMin)),
            AdminStatusFilter.Disconnected => found.Where(d => d.AuthTokenRevokedAt == null && d.LastReportAt != null && d.LastReportAt < now.AddMinutes(-d.InactivityThresholdMin)),
            AdminStatusFilter.NeverReported => found.Where(d => d.AuthTokenRevokedAt == null && d.LastReportAt == null),
            AdminStatusFilter.Decommissioned => found.Where(d => d.AuthTokenRevokedAt != null),
            _ => found,
        };

        // Sequential on purpose: one DbContext cannot run two queries at once
        var total = await found.CountAsync(ct);
        var rows = await found.OrderBy(d => d.SerialNumber).Skip(query.Page * query.Size).Take(query.Size).ToListAsync(ct);

        var counts = new AdminDeviceCounts(
            Registered: await all.CountAsync(ct),
            Linked: await all.CountAsync(d => d.PlaceId != null, ct),
            Connected: await all.CountAsync(d => d.AuthTokenRevokedAt == null && d.LastReportAt != null && d.LastReportAt >= now.AddMinutes(-d.InactivityThresholdMin), ct),
            Disconnected: await all.CountAsync(d => d.AuthTokenRevokedAt == null && d.LastReportAt != null && d.LastReportAt < now.AddMinutes(-d.InactivityThresholdMin), ct),
            NeverReported: await all.CountAsync(d => d.AuthTokenRevokedAt == null && d.LastReportAt == null, ct),
            Decommissioned: await all.CountAsync(d => d.AuthTokenRevokedAt != null, ct));

        var items = rows.Select(d => new AdminDeviceRow(
            d.Id, d.SerialNumber, StatusOf(d, now), d.AuthTokenRevokedAt != null, d.PlaceId != null,
            AsUtc(d.LastReportAt), d.FirmwareVersion)).ToList();

        return new AdminDevicePage(items, query.Page, query.Size, total, counts);
    }

    public async Task<AdminDeviceDetail?> GetDetailAsync(Guid id, DateTime now, CancellationToken ct)
    {
        var d = await db.Devices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (d is null) return null;

        var stays = await db.Links.AsNoTracking().Where(l => l.DeviceId == id).OrderByDescending(l => l.LinkedAt).ToListAsync(ct);
        var log = await db.AdminLog.AsNoTracking().Where(l => l.DeviceId == id).OrderByDescending(l => l.OccurredAt).ToListAsync(ct);

        return new AdminDeviceDetail(
            d.Id, d.SerialNumber, StatusOf(d, now), d.AuthTokenRevokedAt != null, AsUtc(d.AuthTokenRevokedAt),
            d.PlaceId, AsUtc(d.LinkedAt), AsUtc(d.LastReportAt), d.FirmwareVersion, AsUtc(d.CreatedAt),
            AsUtc(d.AuthTokenLastUsedAt), d.PairingFailedAttempts,
            stays.Select(l => new AdminPlaceStay(l.PlaceId, AsUtc(l.LinkedAt), AsUtc(l.UnlinkedAt))).ToList(),
            log.Select(l => new AdminLogEntry(ActionOf(l.Action), l.AdminUserId, l.AdminName, AsUtc(l.OccurredAt))).ToList());
    }

    public async Task<Device?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        return entity is null ? null : DeviceMapper.ToDomain(entity);
    }

    public Task<string?> GetLastFactorySerialAsync(CancellationToken ct) =>
        db.Devices.AsNoTracking()
            .Where(d => d.SerialNumber.StartsWith(FactorySerials.Prefix))
            .OrderByDescending(d => d.SerialNumber)
            .Select(d => (string?)d.SerialNumber)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(IReadOnlyList<Device> devices, AdminLogEntry log, CancellationToken ct)
    {
        var serials = devices.Select(d => d.SerialNumber).ToList();
        var taken = await db.Devices.AsNoTracking().Where(d => serials.Contains(d.SerialNumber)).Select(d => d.SerialNumber).FirstOrDefaultAsync(ct);
        if (taken is not null) throw new SerialAlreadyExistsException(taken);

        // Devices first, then their log rows (the log has a foreign key to the device): both inside ONE transaction
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var device in devices) db.Devices.Add(DeviceMapper.ToEntity(device));
            await db.SaveChangesAsync(ct);

            foreach (var device in devices) db.AdminLog.Add(LogRow(device.Id, log));
            await db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // UQ_devices_serial: another administrator registered the same serial a moment before
            throw new SerialAlreadyExistsException(serials[0]);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    public async Task SaveCredentialsAsync(Device device, AdminLogEntry log, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Devices.Where(d => d.Id == device.Id).ExecuteUpdateAsync(s => s
            .SetProperty(d => d.AuthTokenHash, device.AuthTokenHash)
            .SetProperty(d => d.AuthTokenLastUsedAt, device.AuthTokenLastUsedAt)
            .SetProperty(d => d.PairingCodeHash, device.PairingCodeHash)
            .SetProperty(d => d.PairingFailedAttempts, device.PairingFailedAttempts)
            .SetProperty(d => d.PairingLockedUntil, device.PairingLockedUntil)
            .SetProperty(d => d.UpdatedAt, device.UpdatedAt), ct);
        db.AdminLog.Add(LogRow(device.Id, log));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        db.ChangeTracker.Clear();
    }

    public async Task SaveDecommissionAsync(Device device, AdminLogEntry log, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Only if nobody linked it in the meantime
        var changed = await db.Devices.Where(d => d.Id == device.Id && d.PlaceId == null).ExecuteUpdateAsync(s => s
            .SetProperty(d => d.AuthTokenRevokedAt, device.AuthTokenRevokedAt)
            .SetProperty(d => d.UpdatedAt, device.UpdatedAt), ct);
        if (changed == 0) throw new DeviceStillLinkedException(device.SerialNumber);

        db.AdminLog.Add(LogRow(device.Id, log));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        db.ChangeTracker.Clear();
    }

    /// <summary>SQL Server 2601 / 2627 = duplicate key; SQLite says UNIQUE in the message (tests).</summary>
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;

    private static DeviceAdminLogEntity LogRow(Guid deviceId, AdminLogEntry log) => new()
    {
        Id = Guid.NewGuid(),
        DeviceId = deviceId,
        Action = ActionToDb(log.Action),
        AdminUserId = log.AdminUserId,
        AdminName = log.AdminName is { Length: > 150 } name ? name[..150] : log.AdminName,
        OccurredAt = log.OccurredAt,
    };

    private static string ActionToDb(AdminAction action) => action switch
    {
        AdminAction.Registered => "REGISTERED",
        AdminAction.CredentialsRegenerated => "CREDENTIALS_REGENERATED",
        AdminAction.Decommissioned => "DECOMMISSIONED",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    private static AdminAction ActionOf(string value) => value switch
    {
        "REGISTERED" => AdminAction.Registered,
        "CREDENTIALS_REGENERATED" => AdminAction.CredentialsRegenerated,
        "DECOMMISSIONED" => AdminAction.Decommissioned,
        _ => throw new InvalidOperationException($"Unknown admin action '{value}' in the database."),
    };

    /// <summary>Same rule as Device.StatusAt: connected = reported inside the device own threshold.</summary>
    private static DeviceStatus StatusOf(DeviceEntity d, DateTime now) =>
        d.LastReportAt is null ? DeviceStatus.NeverReported
        : now - AsUtc(d.LastReportAt.Value) > TimeSpan.FromMinutes(d.InactivityThresholdMin) ? DeviceStatus.Disconnected
        : DeviceStatus.Connected;

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? AsUtc(DateTime? value) => value is null ? null : AsUtc(value.Value);
}
