namespace SyWater.Devices.Infrastructure.Places;

/// <summary>
/// Gives the access token of the request being processed. Implemented in the Api project
/// (it reads the Authorization header), so Infrastructure does not depend on ASP.NET Core.
/// </summary>
public interface IAccessTokenProvider
{
    string? GetAccessToken();
}
