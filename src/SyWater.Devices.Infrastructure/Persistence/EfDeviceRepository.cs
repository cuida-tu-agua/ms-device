using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Infrastructure.Persistence;

/// <summary>
/// Outbound adapter: implements IDeviceRepository with EF Core + SQL Server.
/// Writes use ExecuteUpdateAsync (one UPDATE with only the columns of that operation), so a heartbeat
/// and a link that arrive at the same time never overwrite each other.
/// </summary>
public sealed class EfDeviceRepository(DevicesDbContext db) : IDeviceRepository
{
    public async Task<Device?> GetBySerialAsync(string serialNumber, CancellationToken ct)
    {
        var entity = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber, ct);   // UQ_devices_serial
        return entity is null ? null : DeviceMapper.ToDomain(entity);
    }

    public async Task<Device?> GetByPlaceAsync(Guid placeId, CancellationToken ct)
    {
        var entity = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.PlaceId == placeId, ct);             // UX_devices_place
        return entity is null ? null : DeviceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Device>> GetLinkedByAsync(Guid userId, CancellationToken ct)
    {
        var entities = await db.Devices.AsNoTracking()
            .Where(d => d.LinkedBy == userId && d.PlaceId != null)
            .ToListAsync(ct);
        return entities.Select(DeviceMapper.ToDomain).ToList();
    }

    public async Task<DeviceMetrics> GetMetricsAsync(DateTime now, CancellationToken ct)
    {
        // Sequential on purpose: one DbContext cannot run two queries at once
        var total = await db.Devices.CountAsync(ct);
        var linked = await db.Devices.CountAsync(d => d.PlaceId != null, ct);
        // Same rule as Device.StatusAt: connected = last report inside the device's own threshold
        var connected = await db.Devices.CountAsync(
            d => d.LastReportAt != null && d.LastReportAt >= now.AddMinutes(-d.InactivityThresholdMin), ct);
        return new DeviceMetrics(total, connected, linked);
    }

    public Task SaveHeartbeatAsync(Device device, CancellationToken ct) =>
        db.Devices.Where(d => d.Id == device.Id).ExecuteUpdateAsync(s => s
            .SetProperty(d => d.LastReportAt, device.LastReportAt)
            .SetProperty(d => d.AuthTokenLastUsedAt, device.AuthTokenLastUsedAt)
            .SetProperty(d => d.Status, DeviceMapper.ToDb(device.Status))
            .SetProperty(d => d.FirmwareVersion, device.FirmwareVersion)
            .SetProperty(d => d.UpdatedAt, device.UpdatedAt), ct);

    public Task SaveFailedPairingAsync(Device device, PairingPolicy policy, DateTime now, CancellationToken ct)
    {
        // Same rule as Device.RegisterFailedPairing, but computed by SQL Server from the CURRENT row:
        //   attempts = attempts + 1 >= max ? 0 : attempts + 1
        //   locked   = attempts + 1 >= max ? now + lock : locked
        // Both CASE expressions read the value BEFORE the update, so parallel requests cannot skip the lock.
        var max = policy.MaxFailedAttempts;
        var lockedUntil = now + policy.LockDuration;

        return db.Devices.Where(d => d.Id == device.Id).ExecuteUpdateAsync(s => s
            .SetProperty(d => d.PairingFailedAttempts,
                d => d.PairingFailedAttempts + 1 >= max ? 0 : d.PairingFailedAttempts + 1)
            .SetProperty(d => d.PairingLockedUntil,
                d => d.PairingFailedAttempts + 1 >= max ? lockedUntil : d.PairingLockedUntil)
            .SetProperty(d => d.UpdatedAt, now), ct);
    }

    public async Task LinkAsync(Device device, DeviceLink link, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // "AND place_id IS NULL": if another request linked this device a moment ago, 0 rows change.
            var changed = await db.Devices
                .Where(d => d.Id == device.Id && d.PlaceId == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.PlaceId, device.PlaceId)
                    .SetProperty(d => d.LinkedBy, device.LinkedBy)
                    .SetProperty(d => d.LinkedAt, device.LinkedAt)
                    .SetProperty(d => d.PairingFailedAttempts, device.PairingFailedAttempts)
                    .SetProperty(d => d.PairingLockedUntil, device.PairingLockedUntil)
                    .SetProperty(d => d.UpdatedAt, device.UpdatedAt), ct);

            if (changed == 0) throw new DeviceAlreadyLinkedException(device.SerialNumber);

            db.Links.Add(DeviceMapper.ToEntity(link));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            // UX_devices_place: another device was linked to this place at the same time.
            throw new PlaceAlreadyHasDeviceException(link.PlaceId);
        }
        // Any other exception: "await using" disposes the transaction without commit = rollback.
    }

    public async Task UnlinkAsync(Device device, Guid placeId, Guid unlinkedBy, DateTime unlinkedAt, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var changed = await db.Devices
            .Where(d => d.Id == device.Id && d.PlaceId == placeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.PlaceId, (Guid?)null)
                .SetProperty(d => d.LinkedBy, (Guid?)null)
                .SetProperty(d => d.LinkedAt, (DateTime?)null)
                .SetProperty(d => d.UpdatedAt, unlinkedAt), ct);

        if (changed == 0) throw new DeviceNotLinkedException(placeId); // someone unlinked it first

        await db.Links
            .Where(l => l.DeviceId == device.Id && l.UnlinkedAt == null)          // UX_linkh_device_active
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.UnlinkedAt, unlinkedAt)
                .SetProperty(l => l.UnlinkedBy, unlinkedBy), ct);

        await tx.CommitAsync(ct);
    }

    public Task<int> MarkStaleAsDisconnectedAsync(DateTime now, CancellationToken ct) =>
        db.Devices
            .Where(d => d.Status == DeviceMapper.Connected
                        && d.LastReportAt < now.AddMinutes(-d.InactivityThresholdMin)) // IX_devices_status_report
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, DeviceMapper.Disconnected)
                .SetProperty(d => d.UpdatedAt, now), ct);

    /// <summary>2601 = duplicate key in a unique index, 2627 = unique constraint.</summary>
    private static bool IsUniqueViolation(Exception ex) =>
        (ex as SqlException ?? ex.InnerException as SqlException) is { Number: 2601 or 2627 };
}
