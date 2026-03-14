using System.Text.Json;

namespace eCommerce.Api.Services.Payments.PayPal;

public interface IPayPalService
{
    Task<PayPalCreateOrderResult> CreateOrderAsync(PayPalCreateOrderRequest request, CancellationToken cancellationToken);
    Task<PayPalCaptureOrderResult> CaptureOrderAsync(string paypalOrderId, CancellationToken cancellationToken);
    Task<PayPalWebhookVerificationResult> VerifyWebhookAsync(PayPalWebhookVerificationRequest request, CancellationToken cancellationToken);
}

public sealed record PayPalCreateOrderRequest(int OrderId, decimal Amount, string Currency, string Description);

public sealed record PayPalCreateOrderResult(
    bool IsSuccess,
    string? PayPalOrderId,
    string? Status,
    string? ApprovalUrl,
    string? RawResponse,
    string? ErrorMessage);

public sealed record PayPalCaptureOrderResult(
    bool IsSuccess,
    string? PayPalOrderId,
    string? CaptureId,
    string? Status,
    string? PayerEmail,
    string? RawResponse,
    string? ErrorMessage);

public sealed record PayPalWebhookVerificationRequest(
    string TransmissionId,
    string TransmissionTime,
    string TransmissionSignature,
    string CertUrl,
    string AuthAlgorithm,
    JsonElement WebhookEvent);

public sealed record PayPalWebhookVerificationResult(bool IsValid, string VerificationStatus, string? ErrorMessage);
