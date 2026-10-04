using System.ComponentModel.DataAnnotations;

namespace SyWater.Devices.Api.Contracts;

public sealed class LinkDeviceRequest
{
    [Required, StringLength(32)] public string? SerialNumber { get; init; }

    [Required, StringLength(16)] public string? PairingCode { get; init; }
}

public sealed class ValveCommandRequest
{
    [Required] public Guid? CommandId { get; init; }

    [Required] public SyWater.Devices.Application.Valve.ValveAction? Action { get; init; }
}
