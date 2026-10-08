using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Ports.Out;

/// <summary>Outbound port: persistence of devices and their link history.</summary>
public interface IDeviceRepository
{
    Task<Device?> GetBySerialAsync(string serialNumber, CancellationToken ct);

    /// <summary>The device currently linked to a place, or null.</summary>
    Task<Device?> GetByPlaceAsync(Guid placeId, CancellationToken ct);

    /// <summary>Every device currently linked by this user (they may own several places).</summary>
    Task<IReadOnlyList<Device>> GetLinkedByAsync(Guid userId, CancellationToken ct);

    // Each write touches ONLY its own columns. A heartbeat (every 30 s) that saved the whole row
    // could overwrite a link made a millisecond before, so there is no generic "Update".

    /// <summary>Saves last_report_at, status, firmware and token usage of an accepted heartbeat.</summary>
    Task SaveHeartbeatAsync(Device device, CancellationToken ct);

    /// <summary>
    /// Adds one failed pairing attempt and locks at the limit (same rule as Device.RegisterFailedPairing),
    /// ATOMICALLY in the database: ten parallel wrong codes count as ten, not as one.
    /// </summary>
    Task SaveFailedPairingAsync(Device device, PairingPolicy policy, DateTime now, CancellationToken ct);

    /// <summary>
    /// Saves the link AND inserts the new history row, in ONE transaction.
    /// Throws DeviceAlreadyLinkedException / PlaceAlreadyHasDeviceException if another request won the race.
    /// </summary>
    Task LinkAsync(Device device, DeviceLink link, CancellationToken ct);

    /// <summary>Clears the link of <paramref name="placeId"/> AND closes the open history row, in ONE transaction.</summary>
    Task UnlinkAsync(Device device, Guid placeId, Guid unlinkedBy, DateTime unlinkedAt, CancellationToken ct);

    /// <summary>CONNECTED devices whose last report is older than their threshold become DISCONNECTED.</summary>
    Task<int> MarkStaleAsDisconnectedAsync(DateTime now, CancellationToken ct);
}
