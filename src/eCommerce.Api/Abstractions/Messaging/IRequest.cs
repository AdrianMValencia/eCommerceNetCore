namespace eCommerce.Api.Abstractions.Messaging;

/// <summary>
/// Interfaz base para todas las solicitudes (requests) en el sistema.
/// Implementa el patrón Mediator para desacoplar las operaciones de su ejecución.
/// TResponse: El tipo de dato que devolverá esta solicitud cuando sea procesada.
/// 'out' indica que TResponse es covariante (puede devolver tipos más específicos).
/// </summary>
/// <typeparam name="TResponse">Tipo de respuesta que retornará la solicitud</typeparam>
public interface IRequest<out TResponse> { }

/// <summary>
/// Interfaz para operaciones de ESCRITURA (Command en el patrón CQRS).
/// Los comandos modifican el estado del sistema (crear, actualizar, eliminar).
/// Ejemplos: CrearProductoCommand, ActualizarUsuarioCommand, EliminarOrdenCommand.
/// Hereda de IRequest para mantener una estructura común de solicitudes.
/// </summary>
/// <typeparam name="TResponse">Tipo de dato que retornará el comando después de ejecutarse</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>
/// Interfaz para operaciones de LECTURA (Query en el patrón CQRS).
/// Las queries solo consultan datos sin modificar el estado del sistema.
/// Ejemplos: ObtenerProductoPorIdQuery, ListarUsuariosQuery, BuscarOrdenesQuery.
/// CQRS (Command Query Responsibility Segregation) separa las operaciones de lectura y escritura.
/// </summary>
/// <typeparam name="TResponse">Tipo de dato que retornará la consulta</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse> { }