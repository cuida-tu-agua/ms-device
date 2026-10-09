using SyWater.Devices.Application.Devices;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Ports.Out;

/// <summary>Persistence of the admin panel of meters. Every write is ONE transaction with its log row.</summary>
public interface IAdminDeviceRepository
{
    Task<AdminDevicePage> ListAsync(AdminDeviceQuery query, DateTime now, CancellationToken ct);

    Task<AdminDeviceDetail?> GetDetailAsync(Guid id, DateTime now, CancellationToken ct);

    Task<Device?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>The highest serial of the factory sequence (SW-ESP32-...), or null if there is none.</summary>
    Task<string?> GetLastFactorySerialAsync(CancellationToken ct);

    /// <summary>Inserts the devices and one "registered" log row each. Throws SerialAlreadyExistsException if a serial is taken.</summary>
    Task AddAsync(IReadOnlyList<Device> devices, AdminLogEntry log, CancellationToken ct);

    /// <summary>Saves the new secrets of the device and the log row.</summary>
    Task SaveCredentialsAsync(Device device, AdminLogEntry log, CancellationToken ct);

    /// <summary>Saves the decommission and the log row.</summary>
    Task SaveDecommissionAsync(Device device, AdminLogEntry log, CancellationToken ct);
}
