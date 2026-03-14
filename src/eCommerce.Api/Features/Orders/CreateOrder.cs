using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Enums;
using eCommerce.Api.Shared.Bases;
using eCommerce.Api.Shared.Security;
using FluentValidation;
using Npgsql;

namespace eCommerce.Api.Features.Orders;

public class CreateOrder
{
    #region Command
    public sealed class Command : ICommand<OrderCreatedResponse>
    {
        public int UserId { get; set; }
        public List<CreateOrderDetail> OrderDetails { get; set; } = new();
    }

    public sealed class CreateOrderDetail
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public sealed class OrderCreatedResponse
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
        public string OrderState { get; set; } = null!;
    }
    #endregion

    #region Validator
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.");

            RuleFor(x => x.OrderDetails)
                .NotEmpty().WithMessage("OrderDetails is required.");

            RuleForEach(x => x.OrderDetails)
                .ChildRules(detail =>
                {
                    detail.RuleFor(x => x.ProductId)
                        .GreaterThan(0).WithMessage("ProductId must be greater than 0.");

                    detail.RuleFor(x => x.Quantity)
                        .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

                    detail.RuleFor(x => x.Price)
                        .GreaterThan(0).WithMessage("Price must be greater than 0.");
                });
        }
    }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : ICommandHandler<Command, OrderCreatedResponse>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<OrderCreatedResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                command,
                async () => await CreateOrderAsync(command, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<OrderCreatedResponse>> CreateOrderAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<OrderCreatedResponse>();

            const string sqlOrder = @"INSERT INTO public.""Orders"" (""OrderDate"", ""OrderState"", ""UserId"", ""Total"") VALUES (@OrderDate, @OrderState, @UserId, @Total) RETURNING ""OrderId"";";
            const string sqlOrderDetail = @"INSERT INTO public.""OrderDetails"" (""OrderId"", ""ProductId"", ""Quantity"", ""Price"") VALUES (@OrderId, @ProductId, @Quantity, @Price);";

            await using var connection = (NpgsqlConnection)_context.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var total = command.OrderDetails.Sum(detail => detail.Price * detail.Quantity);
                var orderState = OrderState.PENDING_PAYMENT.ToString();

                var orderId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sqlOrder, new
                {
                    OrderDate = DateTime.UtcNow,
                    OrderState = orderState,
                    UserId = command.UserId,
                    Total = total
                }, transaction, cancellationToken: cancellationToken));

                foreach (var detail in command.OrderDetails)
                {
                    await connection.ExecuteAsync(new CommandDefinition(sqlOrderDetail, new
                    {
                        OrderId = orderId,
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        Price = detail.Price * detail.Quantity
                    }, transaction, cancellationToken: cancellationToken));
                }

                await transaction.CommitAsync(cancellationToken);

                response.IsSuccess = true;
                response.Data = new OrderCreatedResponse
                {
                    OrderId = orderId,
                    Total = total,
                    OrderState = orderState
                };
                response.Message = "Orden creada correctamente y pendiente de pago.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
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
                var response = await dispatcher.Dispatch<Command, OrderCreatedResponse>(command, cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(AuthPolicies.AdminAccess);
        }
    }
    #endregion
}
