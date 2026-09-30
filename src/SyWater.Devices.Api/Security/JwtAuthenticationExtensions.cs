using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SyWater.Devices.Api.Security;

/// <summary>
/// Validates the access tokens issued by ms-iam (RS256). device-service only needs the
/// PUBLIC key: it can verify signatures but can never issue tokens.
/// </summary>
public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddIamJwtAuthentication(
        this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var section = config.GetSection("Jwt");
        var issuer = section["Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer.");
        var keyPath = section["PublicKeyPath"] ?? throw new InvalidOperationException("Missing Jwt:PublicKeyPath.");

        var fullPath = Path.IsPathRooted(keyPath) ? keyPath : Path.Combine(AppContext.BaseDirectory, keyPath);
        var rsa = RSA.Create(); // not disposed on purpose: the key lives as long as the app
        rsa.ImportFromPem(File.ReadAllText(fullPath));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !env.IsDevelopment();
                options.MapInboundClaims = false; // keep "sub" as "sub" (no SOAP-style claim names)
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = false, // ms-iam does not emit "aud" yet (see guide)
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = section["KeyId"] },
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    NameClaimType = "sub",
                };
            });

        return services;
    }
}
