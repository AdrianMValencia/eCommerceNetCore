using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Features.Orders;

public class DeleteOrder
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public int OrderId { get; set; }
        public Command(int orderId) => OrderId = orderId;
    }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : ICommandHandler<Command, bool>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<bool>> Handle(Command command, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                command,
                async () => await DeleteOrderAsync(command.OrderId, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<bool>> DeleteOrderAsync(int orderId, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            const string sql = @"DELETE FROM public.Orders WHERE Id = @OrderId;";

            try
            {
                using var connection = _context.CreateConnection();
                var affected = await connection.ExecuteAsync(sql, new { OrderId = orderId });

                response.IsSuccess = affected > 0;
                response.Data = affected > 0;
                response.Message = affected > 0 ? "Orden eliminada correctamente." : "Orden no encontrada.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al eliminar la orden. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class DeleteOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/orders/{id:int}", async (
                IDispatcher dispatcher,
                int id,
                CancellationToken cancellationToken
            ) =>
            {
                var response = await dispatcher.Dispatch<Command, bool>(new Command(id), cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
