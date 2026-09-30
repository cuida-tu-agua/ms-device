using Microsoft.AspNetCore.Diagnostics;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Common;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Api.Errors;

/// <summary>
/// Translates domain exceptions into HTTP responses with the RFC 9457 "problem details" format.
/// The domain never knows about HTTP; this is the only place where that translation happens.
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails, TimeProvider clock) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, code) = exception switch
        {
            InvalidDeviceDataException e => (StatusCodes.Status400BadRequest, e.Code),
            PairingFailedException e => (StatusCodes.Status400BadRequest, e.Code),
            PlaceNotFoundException e => (StatusCodes.Status404NotFound, e.Code),
            DeviceNotLinkedException e => (StatusCodes.Status404NotFound, e.Code),
            DeviceAlreadyLinkedException e => (StatusCodes.Status409Conflict, e.Code),
            PlaceAlreadyHasDeviceException e => (StatusCodes.Status409Conflict, e.Code),
            DeviceOfflineException e => (StatusCodes.Status409Conflict, e.Code),
            DeviceRevokedException e => (StatusCodes.Status409Conflict, e.Code),
            PairingLockedException e => (StatusCodes.Status429TooManyRequests, e.Code),
            ExternalServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "service.unavailable"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "auth.invalid_token"),
            _ => (0, ""),
        };

        if (status == 0) return false; // unknown error: let ASP.NET Core return a generic 500

        if (exception is PairingLockedException locked)
        {
            // Standard header: how many seconds the client should wait.
            var seconds = Math.Max(1, (int)Math.Ceiling((locked.LockedUntilUtc - clock.GetUtcNow().UtcDateTime).TotalSeconds));
            context.Response.Headers.RetryAfter = seconds.ToString();
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = code,
                Detail = exception.Message,
                Type = exception is DomainException ? $"https://sywater.dev/errors/{code}" : null,
            },
        });
    }
}
