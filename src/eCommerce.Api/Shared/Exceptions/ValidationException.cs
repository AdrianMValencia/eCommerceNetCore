using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Shared.Exceptions;

/// <summary>
/// Excepción personalizada que se lanza cuando falla la validación de una solicitud.
/// Hereda de Exception para poder ser capturada en bloques try-catch.
/// Transporta información detallada sobre los errores de validación encontrados.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Colección de errores de validación que causaron esta excepción.
    /// Cada BaseError contiene detalles sobre qué propiedad falló y por qué.
    /// Esta propiedad es de solo lectura (get) para mantener la inmutabilidad de la excepción.
    /// </summary>
    public IEnumerable<BaseError>? Errors { get; }

    /// <summary>
    /// Constructor por defecto sin parámetros.
    /// Llama al constructor base de Exception e inicializa Errors como lista vacía.
    /// Se usa cuando no se tienen errores específicos aún.
    /// </summary>
    public ValidationException() : base()
    {
        // Inicializamos con una lista vacía en lugar de null
        Errors = new List<BaseError>();
    }

    /// <summary>
    /// Constructor que recibe una colección de errores.
    /// Este es el constructor más usado para crear la excepción con errores específicos.
    /// </summary>
    /// <param name="errors">Lista de errores de validación encontrados</param>
    public ValidationException(IEnumerable<BaseError> errors) : this()
    {
        // Asignamos los errores recibidos a la propiedad
        Errors = errors;
    }
}
