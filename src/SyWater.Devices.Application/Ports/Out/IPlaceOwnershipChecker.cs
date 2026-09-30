namespace SyWater.Devices.Application.Ports.Out;

/// <summary>
/// Outbound port: asks place-service whether a place exists and belongs to the user of the
/// current request. device-service never reads the places schema directly (ADR-002 rule 4).
/// </summary>
public interface IPlaceOwnershipChecker
{
    /// <exception cref="ExternalServiceUnavailableException">place-service did not answer.</exception>
    Task<bool> IsOwnedByRequesterAsync(Guid placeId, CancellationToken ct);
}

/// <summary>Another microservice is down or answered with an unexpected error. HTTP 503.</summary>
public sealed class ExternalServiceUnavailableException(string service, Exception? inner = null)
    : Exception($"The service '{service}' is not available right now.", inner)
{
    public string Service { get; } = service;
}
