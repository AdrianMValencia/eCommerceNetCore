using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Features.Orders;

public class UpdateOrder
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public int OrderId { get; set; }
        public List<UpdateOrderDetail> OrderDetails { get; set; } = new();
    }

    public class UpdateOrderDetail
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
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
                async () => await UpdateOrderAsync(command, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<bool>> UpdateOrderAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            const string sqlOrder = @"UPDATE public.Orders SET OrderDate = @OrderDate WHERE Id = @OrderId;";
            const string sqlOrderDetail = @"UPDATE public.OrderDetails SET ProductId = @ProductId, Quantity = @Quantity, Price = @Price WHERE Id = @OrderDetailId;";

            try
            {
                using var connection = _context.CreateConnection();
                await connection.ExecuteAsync(sqlOrder, new
                {
                    OrderDate = DateTime.UtcNow,
                    OrderId = command.OrderId
                });

                foreach (var detail in command.OrderDetails)
                {
                    await connection.ExecuteAsync(sqlOrderDetail, new
                    {
                        OrderDetailId = detail.OrderDetailId,
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        Price = detail.Price
                    });
                }

                response.IsSuccess = true;
                response.Data = true;
                response.Message = "Orden actualizada correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al actualizar la orden. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class UpdateOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("api/orders/{id:int}", async (
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
