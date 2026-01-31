using eCommerce.Api.Shared.Bases;
using FluentValidation;
// Alias para evitar confusión con System.ComponentModel.DataAnnotations.ValidationException
using ValidationException = eCommerce.Api.Shared.Exceptions.ValidationException;

namespace eCommerce.Api.Shared.Behaviors;

/// <summary>
/// Implementación del servicio de validación usando FluentValidation.
/// FluentValidation es una librería popular para crear reglas de validación
/// de forma fluida y legible (ej: RuleFor(x => x.Email).NotEmpty().EmailAddress()).
/// </summary>
/// <param name="serviceProvider">Contenedor DI para obtener los validadores registrados</param>
public class ValidationService(IServiceProvider serviceProvider) : IValidationService
{
    // Campo privado que almacena el proveedor de servicios
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// Método que ejecuta la validación completa de una solicitud.
    /// Busca todos los validadores registrados para el tipo T y los ejecuta.
    /// </summary>
    public async Task ValidationAsync<T>(T request, CancellationToken cancellationToken = default)
    {
        // Obtenemos TODOS los validadores registrados para el tipo T
        // Puede haber múltiples validadores para un mismo tipo (ej: validaciones de negocio, permisos, etc.)
        // GetServices<T>() devuelve IEnumerable con todas las implementaciones registradas
        var validators = _serviceProvider.GetServices<IValidator<T>>();

        // Si no hay validadores registrados para este tipo, no hay nada que validar
        // Salimos temprano del método (early return pattern)
        if (!validators.Any()) return;

        // Creamos el contexto de validación de FluentValidation
        // Este objeto contiene la solicitud y metadatos necesarios para la validación
        var context = new ValidationContext<T>(request);

        // Ejecutamos TODOS los validadores en PARALELO para mejor rendimiento
        // Task.WhenAll() espera a que todos terminen
        // Select() transforma cada validador en una Task de validación
        // El resultado es un array de ValidationResult (uno por cada validador)
        var validationResults = await 
            Task.WhenAll(validators.Select(x => x.ValidateAsync(context, cancellationToken)));

        // Procesamos los resultados de validación usando LINQ:
        var failures = validationResults
            // 1. Filtramos solo los resultados que tienen errores
            .Where(x => x.Errors.Any())
            // 2. Aplanamos la colección: convertimos IEnumerable<IEnumerable<Error>> en IEnumerable<Error>
            .SelectMany(x => x.Errors)
            // 3. Transformamos cada ValidationFailure de FluentValidation a nuestro BaseError
            .Select(err => new BaseError
            {
                PropertyName = err.PropertyName,    // Nombre de la propiedad que falló
                ErrorMessage = err.ErrorMessage      // Mensaje de error descriptivo
            })
            // 4. Materializamos la consulta LINQ en una lista en memoria
            .ToList();

        // Si encontramos al menos un error de validación
        if (failures.Any())
            // Lanzamos nuestra excepción personalizada con todos los errores encontrados
            // Esta excepción será capturada por el Dispatcher o un middleware
            throw new ValidationException(failures);
    }
}
