namespace eCommerce.Api.Shared.Bases;

/// <summary>
/// Clase que representa un error individual en la aplicación.
/// Se usa para proporcionar información detallada sobre errores de validación
/// o problemas durante el procesamiento de solicitudes.
/// Permite identificar exactamente qué propiedad causó el error y por qué.
/// </summary>
public class BaseError
{
    /// <summary>
    /// Nombre de la propiedad o campo que causó el error.
    /// Ejemplo: "Email", "Precio", "NombreProducto"
    /// Nullable (?): puede ser null si el error no está asociado a una propiedad específica.
    /// </summary>
    public string? PropertyName { get; set; }
    
    /// <summary>
    /// Mensaje descriptivo del error para el usuario o desarrollador.
    /// Ejemplo: "El email no tiene un formato válido", "El precio debe ser mayor a 0"
    /// Nullable (?): permite que no haya mensaje en casos excepcionales.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
