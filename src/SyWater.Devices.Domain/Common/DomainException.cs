namespace SyWater.Devices.Domain.Common;

/// <summary>
/// Base class for business-rule violations. The API layer maps each subtype to an HTTP status.
/// </summary>
public abstract class DomainException(string code, string message) : Exception(message)
{
    /// <summary>Stable, machine-readable error code (e.g. "device.already_linked").</summary>
    public string Code { get; } = code;
}
