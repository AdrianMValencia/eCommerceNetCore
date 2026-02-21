using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Entities;
using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Features.Orders;

public class GetByIdOrder
{
    #region Query
    public sealed class Query : IQuery<Order?>
    {
        public int OrderId { get; set; }
        public Query(int orderId) => OrderId = orderId;
    }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : IQueryHandler<Query, Order?>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<Order?>> Handle(Query query, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetOrderAsync(query.OrderId, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<Order?>> GetOrderAsync(int orderId, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<Order?>();

            const string sql = @"SELECT Id, OrderDate, OrderState, UserId, Total FROM public.Orders WHERE Id = @OrderId;";

            try
            {
                using var connection = _context.CreateConnection();
                var order = await connection.QueryFirstOrDefaultAsync<Order>(sql, new { OrderId = orderId });

                response.IsSuccess = order != null;
                response.Data = order;
                response.Message = order != null ? "Orden obtenida correctamente." : "Orden no encontrada.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al obtener la orden. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetByIdOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/orders/{id:int}", async (
                IDispatcher dispatcher,
                int id,
                CancellationToken cancellationToken
            ) =>
            {
                var response = await dispatcher.Dispatch<Query, Order?>(new Query(id), cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
