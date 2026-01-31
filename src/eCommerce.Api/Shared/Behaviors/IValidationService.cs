namespace eCommerce.Api.Shared.Behaviors;

/// <summary>
/// Interfaz del servicio de validación.
/// Define el contrato para validar solicitudes antes de que sean procesadas por los handlers.
/// Permite centralizar toda la lógica de validación en un solo lugar.
/// Se integra con FluentValidation para validaciones declarativas y reutilizables.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Método que ejecuta todas las validaciones configuradas para un tipo de solicitud.
    /// Si encuentra errores de validación, lanza una ValidationException.
    /// Si todo es válido, el método termina sin hacer nada (retorno void).
    /// </summary>
    /// <typeparam name="T">Tipo de solicitud a validar (Command o Query)</typeparam>
    /// <param name="request">La solicitud con los datos a validar</param>
    /// <param name="cancellationToken">Token de cancelación (por defecto: default)</param>
    /// <returns>Task que se completa cuando la validación termina</returns>
    /// <exception cref="ValidationException">Se lanza si hay errores de validación</exception>
    Task ValidationAsync<T>(T request, CancellationToken cancellationToken = default);
}
