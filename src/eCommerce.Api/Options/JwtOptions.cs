namespace eCommerce.Api.Options;

/// <summary>
/// Opciones de configuración para la autenticación JWT.
/// Se cargan desde la sección Jwt del appsettings.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public int ExpirationMinutes { get; set; } = 60;
}
