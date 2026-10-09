using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Devices;

/// <summary>The administrator who does something (from the token: "sub" and "name").</summary>
public sealed record AdminActor(Guid UserId, string? Name);

public enum AdminStatusFilter { All, Connected, Disconnected, NeverReported, Decommissioned }

public enum AdminLinkFilter { All, Linked, Free }

public sealed record AdminDeviceQuery(string? Search, AdminStatusFilter Status, AdminLinkFilter Link, int Page, int Size)
{
    public const int DefaultSize = 20;
    public const int MaxSize = 100;

    public static AdminDeviceQuery Normalize(string? search, AdminStatusFilter status, AdminLinkFilter link, int? page, int? size) =>
        new(string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToUpperInvariant(), status, link,
            Math.Max(page ?? 0, 0), Math.Clamp(size ?? DefaultSize, 1, MaxSize));
}

/// <summary>Numbers of the top cards. Decommissioned devices are counted apart, never as connected / disconnected / never reported.</summary>
public sealed record AdminDeviceCounts(int Registered, int Linked, int Connected, int Disconnected, int NeverReported, int Decommissioned);

public sealed record AdminDeviceRow(
    Guid Id, string SerialNumber, DeviceStatus Status, bool Decommissioned, bool Linked, DateTime? LastReportAt, string? FirmwareVersion);

public sealed record AdminDevicePage(IReadOnlyList<AdminDeviceRow> Items, int Page, int Size, int TotalItems, AdminDeviceCounts Counts)
{
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)Size);
}

public sealed record AdminPlaceStay(Guid PlaceId, DateTime From, DateTime? Until);

public sealed record AdminLogEntry(AdminAction Action, Guid AdminUserId, string? AdminName, DateTime OccurredAt);

/// <summary>Everything the admin sees about one device (never the hashes).</summary>
public sealed record AdminDeviceDetail(
    Guid Id,
    string SerialNumber,
    DeviceStatus Status,
    bool Decommissioned,
    DateTime? DecommissionedAt,
    Guid? PlaceId,
    DateTime? LinkedAt,
    DateTime? LastReportAt,
    string? FirmwareVersion,
    DateTime CreatedAt,
    DateTime? TokenLastUsedAt,
    int FailedPairingAttempts,
    IReadOnlyList<AdminPlaceStay> Places,
    IReadOnlyList<AdminLogEntry> Log);

/// <summary>Shown ONCE, when the device is registered or its credentials are renewed.</summary>
public sealed record FactoryDeviceView(Guid Id, string SerialNumber, string PairingCode, string AuthToken);

/// <summary>Register 1..50 devices: the next serials of the sequence, or one specific serial.</summary>
public sealed record RegisterDevicesCommand(int? Count, string? SerialNumber);
