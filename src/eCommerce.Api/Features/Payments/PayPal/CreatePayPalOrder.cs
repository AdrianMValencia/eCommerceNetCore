using Carter;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Enums;
using eCommerce.Api.Services.Payments.PayPal;
using eCommerce.Api.Shared.Bases;
using eCommerce.Api.Shared.Security;
using FluentValidation;

namespace eCommerce.Api.Features.Payments.PayPal;

public class CreatePayPalOrder
{
    #region Command
    public sealed class Command : ICommand<Response>
    {
        public int OrderId { get; set; }
    }

    public sealed class Response
    {
        public int OrderId { get; set; }
        public string PayPalOrderId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string ApprovalUrl { get; set; } = null!;
        public string Currency { get; set; } = null!;
        public decimal Amount { get; set; }
    }
    #endregion

    #region Validator
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId)
                .GreaterThan(0).WithMessage("OrderId must be greater than 0.");
        }
    }
    #endregion

    #region Handler
    internal sealed class Handler(
        IPayPalService payPalService,
        IPayPalPaymentStore paymentStore,
        IConfiguration configuration,
        HandlerExecutor executor) : ICommandHandler<Command, Response>
    {
        private readonly IPayPalService _payPalService = payPalService;
        private readonly IPayPalPaymentStore _paymentStore = paymentStore;
        private readonly IConfiguration _configuration = configuration;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                command,
                async () => await CreateAsync(command, cancellationToken),
                cancellationToken);
        }

        private async Task<BaseResponse<Response>> CreateAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<Response>();
            var order = await _paymentStore.GetOrderAsync(command.OrderId, cancellationToken);

            if (order is null)
            {
                response.IsSuccess = false;
                response.Message = "La orden no existe.";
                return response;
            }

            if (order.OrderState == OrderState.PAID.ToString())
            {
                response.IsSuccess = false;
                response.Message = "La orden ya fue pagada.";
                return response;
            }

            var currency = _configuration["PayPal:Currency"] ?? "USD";
            var payPalResult = await _payPalService.CreateOrderAsync(
                new PayPalCreateOrderRequest(command.OrderId, order.Total, currency, $"Pago de orden #{command.OrderId}"),
                cancellationToken);

            if (!payPalResult.IsSuccess || string.IsNullOrWhiteSpace(payPalResult.PayPalOrderId) || string.IsNullOrWhiteSpace(payPalResult.ApprovalUrl))
            {
                response.IsSuccess = false;
                response.Message = payPalResult.ErrorMessage ?? "No fue posible generar la orden de pago en PayPal.";
                response.Errors = [new BaseError { PropertyName = "PayPal", ErrorMessage = response.Message }];
                return response;
            }

            await _paymentStore.UpsertCreatedPaymentAsync(
                new CreatePaymentPersistenceRequest(
                    command.OrderId,
                    payPalResult.PayPalOrderId,
                    currency,
                    order.Total,
                    payPalResult.Status ?? "CREATED",
                    payPalResult.ApprovalUrl,
                    payPalResult.RawResponse ?? string.Empty),
                cancellationToken);

            response.IsSuccess = true;
            response.Data = new Response
            {
                OrderId = command.OrderId,
                PayPalOrderId = payPalResult.PayPalOrderId,
                Status = payPalResult.Status ?? "CREATED",
                ApprovalUrl = payPalResult.ApprovalUrl,
                Currency = currency,
                Amount = order.Total
            };
            response.Message = "Orden de pago PayPal creada correctamente.";
            return response;
        }
    }
    #endregion

    #region Endpoint
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/payments/paypal/orders", async (
                Command command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var response = await dispatcher.Dispatch<Command, Response>(command, cancellationToken);
                return response.IsSuccess ? Results.Ok(response) : Results.BadRequest(response);
            })
            .RequireAuthorization(AuthPolicies.AdminAccess);
        }
    }
    #endregion
}
