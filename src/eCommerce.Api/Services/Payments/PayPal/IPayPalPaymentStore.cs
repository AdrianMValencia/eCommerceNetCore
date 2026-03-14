namespace eCommerce.Api.Services.Payments.PayPal;

public interface IPayPalPaymentStore
{
    Task<OrderPaymentOrderInfo?> GetOrderAsync(int orderId, CancellationToken cancellationToken);
    Task<OrderPaymentRecord?> GetByPayPalOrderIdAsync(string paypalOrderId, CancellationToken cancellationToken);
    Task UpsertCreatedPaymentAsync(CreatePaymentPersistenceRequest request, CancellationToken cancellationToken);
    Task MarkPaymentCapturedAsync(CapturePaymentPersistenceRequest request, CancellationToken cancellationToken);
    Task MarkPaymentFailedAsync(string paypalOrderId, string status, string? rawCaptureResponse, CancellationToken cancellationToken);
    Task LogWebhookEventAsync(PayPalWebhookEventLogEntry entry, CancellationToken cancellationToken);
}

public sealed record OrderPaymentOrderInfo(int OrderId, decimal Total, string OrderState);

public sealed record OrderPaymentRecord(int OrderId, string ProviderOrderId, string Status);

public sealed record CreatePaymentPersistenceRequest(
    int OrderId,
    string ProviderOrderId,
    string Currency,
    decimal Amount,
    string Status,
    string ApprovalUrl,
    string RawCreateResponse);

public sealed record CapturePaymentPersistenceRequest(
    int OrderId,
    string ProviderOrderId,
    string ProviderCaptureId,
    string Status,
    string? PayerEmail,
    string RawCaptureResponse,
    string OrderState);

public sealed record PayPalWebhookEventLogEntry(
    string? EventId,
    string? EventType,
    string VerificationStatus,
    string? ProviderOrderId,
    string Payload,
    string Headers);
