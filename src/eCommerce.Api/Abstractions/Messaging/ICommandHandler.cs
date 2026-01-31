using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Abstractions.Messaging;

/// <summary>
/// Interfaz para los manejadores (handlers) de comandos.
/// Define el contrato que deben cumplir todas las clases que procesen comandos.
/// Cada comando tendrá su propio CommandHandler que implementa esta interfaz.
/// </summary>
/// <typeparam name="TCommand">El tipo de comando que este handler procesará (ej: CrearProductoCommand)</typeparam>
/// <typeparam name="TResponse">El tipo de dato que devolverá después de ejecutar el comando</typeparam>
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Método que ejecuta la lógica de negocio del comando.
    /// Es asíncrono (async/await) para no bloquear el hilo principal.
    /// Retorna un BaseResponse que encapsula el resultado, errores y estado.
    /// </summary>
    /// <param name="command">El comando con los datos necesarios para la operación</param>
    /// <param name="cancellationToken">Token para cancelar la operación si es necesario (ej: timeout, cierre de app)</param>
    /// <returns>Una respuesta base con el resultado de la operación y su estado</returns>
    Task<BaseResponse<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
