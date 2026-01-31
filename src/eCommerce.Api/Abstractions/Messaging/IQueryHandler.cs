using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Abstractions.Messaging;

/// <summary>
/// Interfaz para los manejadores (handlers) de consultas (queries).
/// Define el contrato para todas las clases que procesen queries (operaciones de lectura).
/// A diferencia de CommandHandler, estos NO modifican el estado del sistema.
/// </summary>
/// <typeparam name="TQuery">El tipo de query que este handler procesará (ej: ObtenerProductoPorIdQuery)</typeparam>
/// <typeparam name="TResponse">El tipo de dato que devolverá la consulta (ej: ProductoDto)</typeparam>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Método que ejecuta la lógica de lectura de datos.
    /// Solo consulta, NO modifica datos (principio de CQRS).
    /// Es asíncrono para permitir operaciones de base de datos eficientes.
    /// </summary>
    /// <param name="query">La consulta con los parámetros/filtros necesarios</param>
    /// <param name="cancellationToken">Token para cancelar la operación si el cliente se desconecta o hay timeout</param>
    /// <returns>Una respuesta base con los datos solicitados</returns>
    Task<BaseResponse<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
