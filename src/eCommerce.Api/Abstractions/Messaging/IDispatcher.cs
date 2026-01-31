using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Abstractions.Messaging;

/// <summary>
/// Interfaz del Despachador (Dispatcher) que implementa el patrón Mediator.
/// Su responsabilidad es recibir cualquier solicitud (comando o query) y dirigirla
/// al handler apropiado que sabe cómo procesarla.
/// Esto desacopla el código del cliente (ej: un Controller) de la implementación específica.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Método principal que "despacha" o envía una solicitud al handler correcto.
    /// - Recibe una solicitud genérica (puede ser ICommand o IQuery)
    /// - Identifica automáticamente qué handler debe procesarla
    /// - Invoca ese handler y devuelve su respuesta
    /// - Esto permite que los controladores no necesiten conocer los handlers específicos
    /// </summary>
    /// <typeparam name="TRequest">Tipo de solicitud (debe implementar IRequest)</typeparam>
    /// <typeparam name="TResponse">Tipo de respuesta esperada</typeparam>
    /// <param name="request">La solicitud a procesar (comando o query)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta encapsulada con el resultado de la operación</returns>
    Task<BaseResponse<TResponse>> Dispatch<TRequest, TResponse>(
        TRequest request, 
        CancellationToken cancellationToken) where TRequest : IRequest<TResponse>;
}
