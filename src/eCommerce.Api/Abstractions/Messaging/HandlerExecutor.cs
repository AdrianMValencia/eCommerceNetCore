using eCommerce.Api.Shared.Bases;
using eCommerce.Api.Shared.Behaviors;
using eCommerce.Api.Shared.Exceptions;

namespace eCommerce.Api.Abstractions.Messaging;

/// <summary>
/// Clase ejecutora de handlers (en desarrollo).
/// Esta clase estará encargada de ejecutar la lógica de los handlers,
/// probablemente incluyendo:
/// - Validación automática antes de ejecutar el handler
/// - Logging y monitoreo de la ejecución
/// - Manejo de transacciones
/// - Gestión de errores centralizada
/// </summary>
public class HandlerExecutor(
    IValidationService validationService,
    ILogger<HandlerExecutor> logger)
{
    // Campo comentado que indica que se utilizará el servicio de validación
    // IValidationService validará las solicitudes antes de procesarlas
    //private readonly IValidationService
    private readonly IValidationService _validationService = validationService;
    private readonly ILogger<HandlerExecutor> _logger = logger;

    public async Task<BaseResponse<T>> ExecuteAsync<TRequest, T>(
        TRequest request,
        Func<Task<BaseResponse<T>>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            await _validationService.ValidationAsync(request, cancellationToken);

            return await action();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed for request {@Request}. Errors: {@Errors}", request, ex.Errors);

            return new BaseResponse<T>
            {
                IsSuccess = false,
                Message = "Errores de validación",
                Errors = ex.Errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request {@Request}", request);

            return new BaseResponse<T>
            {
                IsSuccess = false,
                Message = "Ocurrió un error inesperado",
                Errors =
                [
                    new() { PropertyName = "Exception", ErrorMessage = ex.Message }
                ]   
            };
        }
    }
}
