using Carter;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Enums;
using eCommerce.Api.Services.Payments.PayPal;
using eCommerce.Api.Shared.Bases;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace eCommerce.Api.Features.Payments.PayPal;

public class PayPalWebhook
{
    #region Command
    public sealed class Command : ICommand<Response>
    {
        public string RawBody { get; set; } = null!;
        public string TransmissionId { get; set; } = null!;
        public string TransmissionTime { get; set; } = null!;
        public string TransmissionSignature { get; set; } = null!;
        public string CertUrl { get; set; } = null!;
        public string AuthAlgorithm { get; set; } = null!;
        public string HeadersJson { get; set; } = null!;
    }

    public sealed class Response
    {
        public string VerificationStatus { get; set; } = null!;
        public string? EventId { get; set; }
        public string? EventType { get; set; }
        public string? PayPalOrderId { get; set; }
        public bool OrderUpdated { get; set; }
    }
    #endregion

    #region Validator
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RawBody).NotEmpty().WithMessage("RawBody is required.");
            RuleFor(x => x.TransmissionId).NotEmpty().WithMessage("PayPal-Transmission-Id is required.");
            RuleFor(x => x.TransmissionTime).NotEmpty().WithMessage("PayPal-Transmission-Time is required.");
            RuleFor(x => x.TransmissionSignature).NotEmpty().WithMessage("PayPal-Transmission-Sig is required.");
            RuleFor(x => x.CertUrl).NotEmpty().WithMessage("PayPal-Cert-Url is required.");
            RuleFor(x => x.AuthAlgorithm).NotEmpty().WithMessage("PayPal-Auth-Algo is required.");
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
                async () => await ProcessAsync(command, cancellationToken),
                cancellationToken);
        }

        private async Task<BaseResponse<Response>> ProcessAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<Response>();
            using var document = JsonDocument.Parse(command.RawBody);
            var root = document.RootElement;

            var verification = await _payPalService.VerifyWebhookAsync(
                new PayPalWebhookVerificationRequest(
                    command.TransmissionId,
                    command.TransmissionTime,
                    command.TransmissionSignature,
                    command.CertUrl,
                    command.AuthAlgorithm,
                    root.Clone()),
                cancellationToken);

            var eventId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            var eventType = root.TryGetProperty("event_type", out var eventTypeElement) ? eventTypeElement.GetString() : null;
            var payPalOrderId = ExtractPayPalOrderId(root);

            await _paymentStore.LogWebhookEventAsync(
                new PayPalWebhookEventLogEntry(
                    eventId,
                    eventType,
                    verification.VerificationStatus,
                    payPalOrderId,
                    command.RawBody,
                    command.HeadersJson),
                cancellationToken);

            if (!verification.IsValid)
            {
                response.IsSuccess = false;
                response.Message = verification.ErrorMessage ?? "Webhook PayPal no verificado.";
                response.Data = new Response
                {
                    VerificationStatus = verification.VerificationStatus,
                    EventId = eventId,
                    EventType = eventType,
                    PayPalOrderId = payPalOrderId,
                    OrderUpdated = false
                };
                return response;
            }

            var orderUpdated = false;
            if (!string.IsNullOrWhiteSpace(payPalOrderId))
            {
                var payment = await _paymentStore.GetByPayPalOrderIdAsync(payPalOrderId, cancellationToken);
                if (payment is not null && eventType == "PAYMENT.CAPTURE.COMPLETED")
                {
                    var captureId = ExtractCaptureId(root);
                    var payerEmail = ExtractPayerEmail(root);
                    await _paymentStore.MarkPaymentCapturedAsync(
                        new CapturePaymentPersistenceRequest(
                            payment.OrderId,
                            payPalOrderId,
                            captureId ?? string.Empty,
                            "COMPLETED",
                            payerEmail,
                            command.RawBody,
                            OrderState.PAID.ToString()),
                        cancellationToken);
                    orderUpdated = true;
                }
                else if (payment is not null && eventType is "CHECKOUT.ORDER.APPROVED" or "PAYMENT.CAPTURE.DENIED" or "PAYMENT.CAPTURE.DECLINED")
                {
                    var status = eventType == "CHECKOUT.ORDER.APPROVED" ? "APPROVED" : "FAILED";
                    await _paymentStore.MarkPaymentFailedAsync(payPalOrderId, status, command.RawBody, cancellationToken);
                }
            }

            response.IsSuccess = true;
            response.Data = new Response
            {
                VerificationStatus = verification.VerificationStatus,
                EventId = eventId,
                EventType = eventType,
                PayPalOrderId = payPalOrderId,
                OrderUpdated = orderUpdated
            };
            response.Message = "Webhook PayPal procesado correctamente.";
            return response;
        }

        private static string? ExtractPayPalOrderId(JsonElement root)
        {
            if (root.TryGetProperty("resource", out var resource))
            {
                if (resource.TryGetProperty("id", out var resourceId) &&
                    root.TryGetProperty("event_type", out var eventType) &&
                    eventType.GetString() == "CHECKOUT.ORDER.APPROVED")
                {
                    return resourceId.GetString();
                }

                if (resource.TryGetProperty("supplementary_data", out var supplementaryData) &&
                    supplementaryData.TryGetProperty("related_ids", out var relatedIds) &&
                    relatedIds.TryGetProperty("order_id", out var orderId))
                {
                    return orderId.GetString();
                }
            }

            return null;
        }

        private static string? ExtractCaptureId(JsonElement root)
        {
            return root.TryGetProperty("resource", out var resource) && resource.TryGetProperty("id", out var captureId)
                ? captureId.GetString()
                : null;
        }

        private static string? ExtractPayerEmail(JsonElement root)
        {
            if (root.TryGetProperty("resource", out var resource) &&
                resource.TryGetProperty("payer", out var payer) &&
                payer.TryGetProperty("email_address", out var email))
            {
                return email.GetString();
            }

            return null;
        }
    }
    #endregion

    #region Endpoint
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/payments/paypal/webhook", async (
                HttpRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                var rawBody = await reader.ReadToEndAsync(cancellationToken);
                request.Body.Position = 0;

                var command = new Command
                {
                    RawBody = rawBody,
                    TransmissionId = request.Headers["PayPal-Transmission-Id"].ToString(),
                    TransmissionTime = request.Headers["PayPal-Transmission-Time"].ToString(),
                    TransmissionSignature = request.Headers["PayPal-Transmission-Sig"].ToString(),
                    CertUrl = request.Headers["PayPal-Cert-Url"].ToString(),
                    AuthAlgorithm = request.Headers["PayPal-Auth-Algo"].ToString(),
                    HeadersJson = JsonSerializer.Serialize(request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()))
                };

                var response = await dispatcher.Dispatch<Command, Response>(command, cancellationToken);
                return response.IsSuccess ? Results.Ok(response) : Results.BadRequest(response);
            })
            .AllowAnonymous();
        }
    }
    #endregion
}
