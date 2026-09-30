using System.ComponentModel.DataAnnotations;

namespace SyWater.Devices.Api.Contracts;

// HTTP request bodies. Nullable + [Required] makes a missing field a 400 instead of a silent default.
// The real format rules (alphabet, length of the code) live in the domain (DeviceSecrets).

/// <summary>POST /api/places/{placeId}/device (HU-012)</summary>
public sealed class LinkDeviceRequest
{
    /// <summary>Printed on the box, e.g. "SW-ESP32-000001".</summary>
    [Required, StringLength(32)] public string? SerialNumber { get; init; }

    /// <summary>Printed on the box, e.g. "4HZX-VCDP" (dashes and spaces are ignored).</summary>
    [Required, StringLength(16)] public string? PairingCode { get; init; }
}
