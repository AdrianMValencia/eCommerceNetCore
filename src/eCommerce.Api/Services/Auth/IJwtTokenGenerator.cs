using eCommerce.Api.Entities;

namespace eCommerce.Api.Services.Auth;

/// <summary>
/// Contrato encargado de generar tokens JWT a partir del usuario autenticado.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
