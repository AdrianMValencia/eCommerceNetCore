namespace eCommerce.Api.Shared.Bases;

/// <summary>
/// Clase genérica que encapsula TODAS las respuestas de la aplicación.
/// Proporciona un formato estándar y consistente para comunicar resultados,
/// ya sean exitosos o con errores.
/// Esto facilita el manejo de respuestas en el frontend y mejora la experiencia del desarrollador.
/// </summary>
/// <typeparam name="T">Tipo de dato que contendrá la respuesta exitosa</typeparam>
public class BaseResponse<T>
{
    /// <summary>
    /// Indica si la operación se completó exitosamente.
    /// true = éxito, false = falló o hubo errores de validación.
    /// El cliente puede verificar esto primero antes de procesar la respuesta.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Los datos devueltos por la operación cuando fue exitosa.
    /// Puede ser un objeto simple (ProductoDto), una colección (List<ProductoDto>), 
    /// un ID (int), o cualquier tipo T.
    /// Será null si la operación falló o no devuelve datos.
    /// </summary>
    public T? Data { get; set; }
    
    /// <summary>
    /// Mensaje general sobre el resultado de la operación.
    /// Ejemplos: "Producto creado exitosamente", "Error al procesar la solicitud"
    /// Útil para mostrar notificaciones al usuario.
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Colección de errores detallados cuando la operación falló.
    /// Cada error incluye la propiedad afectada y su mensaje.
    /// Será null o vacío cuando IsSuccess es true.
    /// El frontend puede iterar esta lista para mostrar errores específicos en cada campo del formulario.
    /// </summary>
    public IEnumerable<BaseError>? Errors { get; set; }
}
