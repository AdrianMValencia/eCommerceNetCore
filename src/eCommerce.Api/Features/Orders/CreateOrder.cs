using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Enums;
using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Features.Orders;

public class CreateOrder
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public int UserId { get; set; }
        public List<CreateOrderDetail> OrderDetails { get; set; } = new();
    }

    public class CreateOrderDetail
    {
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
                async () => await CreateOrderAsync(command, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<bool>> CreateOrderAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            const string sqlOrder = @"INSERT INTO public.Orders (OrderDate, OrderState, UserId, Total) VALUES (@OrderDate, @OrderState, @UserId, @Total) RETURNING Id;";
            const string sqlOrderDetail = @"INSERT INTO public.OrderDetails (OrderId, ProductId, Quantity, Price) VALUES (@OrderId, @ProductId, @Quantity, @Price);";
            try
            {
                using var connection = _context.CreateConnection();
                decimal total = 0;
                foreach (var detail in command.OrderDetails)
                {
                    total += detail.Price * detail.Quantity;
                }
                var orderId = await connection.ExecuteScalarAsync<int>(sqlOrder, new
                {
                    OrderDate = DateTime.UtcNow,
                    OrderState = OrderState.CONFIRMED.ToString(),
                    UserId = command.UserId,
                    Total = total
                });
                foreach (var detail in command.OrderDetails)
                {
                    await connection.ExecuteAsync(sqlOrderDetail, new
                    {
                        OrderId = orderId,
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        Price = detail.Price * detail.Quantity
                    });
                }
                response.IsSuccess = true;
                response.Data = true;
                response.Message = "Orden creada correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al crear la orden. {ex.Message}";
            }
            return response;
        }
    }
    #endregion

    #region Endpoint
    public class CreateOrderEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/orders", async (
                IDispatcher dispatcher,
                Command command,
                CancellationToken cancellationToken
            ) =>
            {
                var response = await dispatcher.Dispatch<Command, bool>(command, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
