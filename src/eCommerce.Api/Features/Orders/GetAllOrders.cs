using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Entities;
using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Features.Orders;

public class GetAllOrders
{
    #region Query
    public sealed class Query : IQuery<IEnumerable<Order>> { }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : IQueryHandler<Query, IEnumerable<Order>>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<IEnumerable<Order>>> Handle(Query query, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetOrdersAsync(cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<IEnumerable<Order>>> GetOrdersAsync(CancellationToken cancellationToken)
        {
            var response = new BaseResponse<IEnumerable<Order>>();

            const string sql = @"SELECT Id, OrderDate, OrderState, UserId, Total FROM public.Orders ORDER BY Id;";

            try
            {
                using var connection = _context.CreateConnection();

                var result = await connection.QueryAsync<Order>(sql);

                response.IsSuccess = true;
                response.Data = result.ToList();
                response.Message = "Órdenes obtenidas correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al obtener las órdenes. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetAllOrdersEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/orders", async (
                IDispatcher dispatcher,
                CancellationToken cancellationToken
            ) =>
            {
                var response = await dispatcher.Dispatch<Query, IEnumerable<Order>>(new Query(), cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
