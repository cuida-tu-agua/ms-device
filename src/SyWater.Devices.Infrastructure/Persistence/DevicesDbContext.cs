using Microsoft.EntityFrameworkCore;
using SyWater.Devices.Infrastructure.Persistence.Entities;

namespace SyWater.Devices.Infrastructure.Persistence;

/// <summary>
/// EF Core session against sy-water-db. The schema is owned by ms-devices-db (Liquibase):
/// this project NEVER creates EF migrations or calls EnsureCreated.
/// Table and column names come from the [Table]/[Column] attributes on the entities.
/// </summary>
public sealed class DevicesDbContext(DbContextOptions<DevicesDbContext> options) : DbContext(options)
{
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<DeviceLinkEntity> Links => Set<DeviceLinkEntity>();
    public DbSet<DeviceAdminLogEntity> AdminLog => Set<DeviceAdminLogEntity>();
}
