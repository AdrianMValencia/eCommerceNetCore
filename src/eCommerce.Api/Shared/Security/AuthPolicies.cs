namespace eCommerce.Api.Shared.Security;

/// <summary>
/// Nombres centralizados de políticas para evitar strings repetidos en los endpoints.
/// </summary>
public static class AuthPolicies
{
    public const string UserAccess = nameof(UserAccess);
    public const string AdminAccess = nameof(AdminAccess);
}
