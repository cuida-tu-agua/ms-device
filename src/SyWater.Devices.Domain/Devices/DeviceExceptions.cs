using SyWater.Devices.Domain.Common;

namespace SyWater.Devices.Domain.Devices;

/// <summary>Serial or pairing code with an invalid format. HTTP 400.</summary>
public sealed class InvalidDeviceDataException(string message)
    : DomainException("device.invalid", message);

/// <summary>
/// Unknown serial OR wrong pairing code. Both cases share ONE error on purpose,
/// so nobody can find out which serials exist by trying. HTTP 400.
/// </summary>
public sealed class PairingFailedException()
    : DomainException("device.pairing_failed", "The serial number or the pairing code is not correct.");

/// <summary>Too many wrong pairing codes: pairing is blocked for a while. HTTP 429.</summary>
public sealed class PairingLockedException(DateTime lockedUntilUtc)
    : DomainException("device.pairing_locked", $"Too many failed attempts. Try again after {lockedUntilUtc:O}.")
{
    public DateTime LockedUntilUtc { get; } = lockedUntilUtc;
}

/// <summary>The device's factory token was revoked (stolen, replaced...). HTTP 409.</summary>
public sealed class DeviceRevokedException(string serialNumber)
    : DomainException("device.revoked", $"Device {serialNumber} was revoked and cannot be linked.");

/// <summary>HU-012: a device can only be linked to ONE place at a time. HTTP 409.</summary>
public sealed class DeviceAlreadyLinkedException(string serialNumber)
    : DomainException("device.already_linked", $"Device {serialNumber} is already linked to a place. Unlink it first.");

/// <summary>The place already has another device. HTTP 409.</summary>
public sealed class PlaceAlreadyHasDeviceException(Guid placeId)
    : DomainException("place.already_has_device", $"Place {placeId} already has a linked device.");

/// <summary>HU-012: the device must be online (authenticated heartbeat) before linking. HTTP 409.</summary>
public sealed class DeviceOfflineException(string serialNumber)
    : DomainException("device.offline", $"Device {serialNumber} has not reported recently. Turn it on and wait until it connects.");

/// <summary>The place has no linked device (or it belongs to another user). HTTP 404.</summary>
public sealed class DeviceNotLinkedException(Guid placeId)
    : DomainException("device.not_linked", $"Place {placeId} has no linked device.");

/// <summary>The place does not exist, is deleted or belongs to another user (answer of ms-places). HTTP 404.</summary>
public sealed class PlaceNotFoundException(Guid placeId)
    : DomainException("place.not_found", $"Place {placeId} was not found.");

/// <summary>Admin panel: the device does not exist. HTTP 404.</summary>
public sealed class DeviceNotFoundException(Guid id)
    : DomainException("device.not_found", $"Device {id} was not found.");

/// <summary>Admin panel: credentials and decommission are only for devices nobody has linked (the owner must unlink first). HTTP 409.</summary>
public sealed class DeviceStillLinkedException(string serialNumber)
    : DomainException("device.still_linked", $"Device {serialNumber} is linked to a place. Its owner must unlink it from the app first.");

/// <summary>Admin panel: the device was already decommissioned. HTTP 409.</summary>
public sealed class DeviceAlreadyDecommissionedException(string serialNumber)
    : DomainException("device.already_decommissioned", $"Device {serialNumber} was already decommissioned.");

/// <summary>Admin panel: a device with that serial number is already registered. HTTP 409.</summary>
public sealed class SerialAlreadyExistsException(string serialNumber)
    : DomainException("device.serial_exists", $"A device with serial number {serialNumber} is already registered.");
