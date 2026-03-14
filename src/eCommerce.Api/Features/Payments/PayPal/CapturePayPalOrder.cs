using Carter;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Enums;
using eCommerce.Api.Services.Payments.PayPal;
using eCommerce.Api.Shared.Bases;
using eCommerce.Api.Shared.Security;
using FluentValidation;

namespace eCommerce.Api.Features.Payments.PayPal;

public class CapturePayPalOrder
{
    #region Command
    public sealed class Command : ICommand<Response>
    {
        public string PayPalOrderId { get; set; } = null!;
    }

    public sealed class Response
    {
        public int OrderId { get; set; }
        public string PayPalOrderId { get; set; } = null!;
        public string CaptureId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string OrderState { get; set; } = null!;
        public string? PayerEmail { get; set; }
    }
    #endregion

    #region Validator
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PayPalOrderId)
                .NotEmpty().WithMessage("PayPalOrderId is required.");
        }
    }
    #endregion

    #region Handler
    internal sealed class Handler(
        IPayPalService payPalService,
        IPayPalPaymentStore paymentStore,
        HandlerExecutor executor) : ICommandHandler<Command, Response>
    {
        private readonly IPayPalService _payPalService = payPalService;
        private readonly IPayPalPaymentStore _paymentStore = paymentStore;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                command,
                async () => await CaptureAsync(command, cancellationToken),
                cancellationToken);
        }

        private async Task<BaseResponse<Response>> CaptureAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<Response>();
            var payment = await _paymentStore.GetByPayPalOrderIdAsync(command.PayPalOrderId, cancellationToken);

            if (payment is null)
            {
                response.IsSuccess = false;
                response.Message = "No existe un pago local asociado al PayPalOrderId proporcionado.";
                return response;
            }

            var captureResult = await _payPalService.CaptureOrderAsync(command.PayPalOrderId, cancellationToken);

            if (!captureResult.IsSuccess || string.IsNullOrWhiteSpace(captureResult.Status))
            {
                await _paymentStore.MarkPaymentFailedAsync(command.PayPalOrderId, "CAPTURE_FAILED", captureResult.RawResponse, cancellationToken);
                response.IsSuccess = false;
                response.Message = captureResult.ErrorMessage ?? "No fue posible capturar la orden en PayPal.";
                response.Errors = [new BaseError { PropertyName = "PayPal", ErrorMessage = response.Message }];
                return response;
            }

            if (captureResult.Status != "COMPLETED" || string.IsNullOrWhiteSpace(captureResult.CaptureId))
            {
                await _paymentStore.MarkPaymentFailedAsync(command.PayPalOrderId, captureResult.Status, captureResult.RawResponse, cancellationToken);
                response.IsSuccess = false;
                response.Message = $"La captura PayPal no quedó completada. Estado actual: {captureResult.Status}.";
                return response;
            }

            await _paymentStore.MarkPaymentCapturedAsync(
                new CapturePaymentPersistenceRequest(
                    payment.OrderId,
                    command.PayPalOrderId,
                    captureResult.CaptureId,
                    captureResult.Status,
                    captureResult.PayerEmail,
                    captureResult.RawResponse ?? string.Empty,
                    OrderState.PAID.ToString()),
                cancellationToken);

            response.IsSuccess = true;
            response.Data = new Response
            {
                OrderId = payment.OrderId,
                PayPalOrderId = command.PayPalOrderId,
                CaptureId = captureResult.CaptureId,
                Status = captureResult.Status,
                OrderState = OrderState.PAID.ToString(),
                PayerEmail = captureResult.PayerEmail
            };
            response.Message = "Pago PayPal capturado correctamente.";
            return response;
        }
    }
    #endregion

    #region Endpoint
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/payments/paypal/orders/{paypalOrderId}/capture", async (
                string paypalOrderId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var response = await dispatcher.Dispatch<Command, Response>(new Command { PayPalOrderId = paypalOrderId }, cancellationToken);
                return response.IsSuccess ? Results.Ok(response) : Results.BadRequest(response);
            })
            .RequireAuthorization(AuthPolicies.AdminAccess);
        }
    }
    #endregion
}
