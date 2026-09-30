using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SyWater.Devices.Api.Security;

/// <summary>
/// HU-012: each USER can try to link at most 10 times every 15 minutes, whatever serial they use.
/// The per-device lock (PairingPolicy) protects one device; this limit stops one account from
/// trying many serials (or locking many devices of other people).
/// </summary>
public static class PairingRateLimiter
{
    public const string Policy = "pairing";

    public static IServiceCollection AddPairingRateLimiter(this IServiceCollection services, IConfiguration config)
    {
        var permits = config.GetValue("Devices:LinkAttemptsPerUser", 10);
        var window = TimeSpan.FromMinutes(config.GetValue("Devices:LinkAttemptsWindowMinutes", 15));

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // One bucket per user ("sub" of the token). The endpoint requires a token, so "sub" always exists.
            options.AddPolicy(Policy, http => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: http.User.FindFirstValue("sub") ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permits,
                    Window = window,
                    QueueLimit = 0,
                }));

            // Same Problem Details shape as every other error, plus Retry-After.
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                var problems = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problems.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "device.too_many_attempts",
                        Detail = "Too many link attempts. Wait before trying again.",
                        Type = "https://sywater.dev/errors/device.too_many_attempts",
                    },
                });
            };
        });
    }
}
