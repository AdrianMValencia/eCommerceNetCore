using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Enums;
using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Features.Orders;

public class UpdateOrderState
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public int OrderId { get; set; }
        public OrderState OrderState { get; set; }
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
                async () => await UpdateOrderStateAsync(command, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<bool>> UpdateOrderStateAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            const string sql = @"UPDATE public.Orders SET OrderState = @OrderState WHERE Id = @OrderId;";

            try
            {
                using var connection = _context.CreateConnection();
                var affected = await connection.ExecuteAsync(sql, new
                {
                    OrderId = command.OrderId,
                    OrderState = command.OrderState.ToString()
                });

                response.IsSuccess = affected > 0;
                response.Data = affected > 0;
                response.Message = affected > 0 ? "Estado de la orden actualizado correctamente." : "Orden no encontrada.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al actualizar el estado de la orden. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class UpdateOrderStateEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("api/orders/{id:int}/state", async (
                IDispatcher dispatcher,
                int id,
                Command command,
                CancellationToken cancellationToken
            ) =>
            {
                command.OrderId = id;
                var response = await dispatcher.Dispatch<Command, bool>(command, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
