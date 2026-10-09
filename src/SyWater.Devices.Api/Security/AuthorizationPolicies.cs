namespace SyWater.Devices.Api.Security;

/// <summary>Named policies. The roles come in the "roles" claim of the ms-iam token.</summary>
public static class AuthorizationPolicies
{
    public const string Admin = "Admin";
    public const string AdminRole = "ADMIN";
}
