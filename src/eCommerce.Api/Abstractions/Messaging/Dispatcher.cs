using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Abstractions.Messaging;

/// <summary>
/// Implementación concreta del Dispatcher (Despachador).
/// Esta clase es el corazón del patrón Mediator en nuestra aplicación.
/// Recibe solicitudes y las dirige al handler correspondiente usando reflexión.
/// </summary>
/// <param name="serviceProvider">Contenedor de inyección de dependencias que proporciona los handlers</param>
public class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    // Campo privado que guarda la referencia al contenedor de servicios
    // El guion bajo (_) es una convención para campos privados
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// Método que implementa la lógica de despacho de solicitudes.
    /// Usa reflexión y tipos genéricos para encontrar y ejecutar el handler correcto.
    /// </summary>
    public async Task<BaseResponse<TResponse>> Dispatch<TRequest, TResponse>
        (TRequest request, CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        try
        {
            // Verificamos si la solicitud es un COMANDO (operación de escritura)
            if (request is ICommand<TResponse>)
            {
                // REFLEXIÓN: Construimos dinámicamente el tipo de handler necesario
                // typeof(ICommandHandler<,>) es una definición genérica abierta
                // MakeGenericType() la cierra con tipos concretos
                // Ejemplo: si request es CrearProductoCommand y TResponse es int
                // esto crea: ICommandHandler<CrearProductoCommand, int>
                var handlerType = typeof(ICommandHandler<,>)
                    .MakeGenericType(request.GetType(), typeof(TResponse));

                // Solicitamos al contenedor de DI que nos proporcione una instancia del handler
                // 'dynamic' permite llamar métodos sin conocer el tipo en tiempo de compilación
                // Esto es necesario porque el tipo exacto se conoce en tiempo de ejecución
                dynamic handler = _serviceProvider.GetRequiredService(handlerType);

                // Ejecutamos el método Handle del handler encontrado
                // (dynamic) convierte la solicitud al tipo esperado por el handler
                return await handler.Handle((dynamic)request, cancellationToken);
            }

            // Verificamos si la solicitud es una QUERY (operación de lectura)
            if (request is IQuery<TResponse>)
            {
                // Mismo proceso que con comandos, pero usando IQueryHandler
                // Ejemplo: si request es ObtenerProductoPorIdQuery y TResponse es ProductoDto
                // esto crea: IQueryHandler<ObtenerProductoPorIdQuery, ProductoDto>
                var handlerType = typeof(IQueryHandler<,>)
                    .MakeGenericType(request.GetType(), typeof(TResponse));

                // Obtenemos la instancia del query handler desde el contenedor DI
                dynamic handler = _serviceProvider.GetRequiredService(handlerType);

                // Ejecutamos el método Handle del query handler
                return await handler.Handle((dynamic)request, cancellationToken);
            }

            // Si llegamos aquí, la solicitud no es ni comando ni query (caso excepcional)
            throw new InvalidOperationException("Tipo de solicitud no compatible.");
        }
        catch (Exception ex)
        {
            // Capturamos cualquier error y lo devolvemos en un formato estándar
            // Esto previene que excepciones no controladas colapsen la aplicación
            return new BaseResponse<TResponse>
            {
                // Indicamos que la operación falló
                IsSuccess = false,
                // Mensaje genérico para el usuario
                Message = "Ocurrió un error al procesar la solicitud.",
                // Lista de errores con detalles técnicos
                // El operador [] es la sintaxis moderna de colección (Collection Expression C# 12+)
                Errors =
                [
                    new() { PropertyName = "Dispatcher", ErrorMessage = ex.Message }
                ]
            };
        }
    }
}
