using Dapper;
using eCommerce.Api.Database;
using Npgsql;

namespace eCommerce.Api.Services.Payments.PayPal;

public sealed class PayPalPaymentStore(
    ApplicationDbContext context,
    ILogger<PayPalPaymentStore> logger) : IPayPalPaymentStore
{
    private const string Provider = "PAYPAL";
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<PayPalPaymentStore> _logger = logger;

    public async Task<OrderPaymentOrderInfo?> GetOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                ""OrderId"",
                ""Total"",
                ""OrderState""
            FROM public.""Orders""
            WHERE ""OrderId"" = @OrderId;";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<OrderPaymentOrderInfo>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));
    }

    public async Task<OrderPaymentRecord?> GetByPayPalOrderIdAsync(string paypalOrderId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                ""OrderId"",
                ""ProviderOrderId"",
                ""Status""
            FROM public.""OrderPayments""
            WHERE ""Provider"" = @Provider AND ""ProviderOrderId"" = @PayPalOrderId;";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<OrderPaymentRecord>(new CommandDefinition(sql, new { Provider, PayPalOrderId = paypalOrderId }, cancellationToken: cancellationToken));
    }

    public async Task UpsertCreatedPaymentAsync(CreatePaymentPersistenceRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO public.""OrderPayments""
            (
                ""OrderId"",
                ""Provider"",
                ""ProviderOrderId"",
                ""Currency"",
                ""Amount"",
                ""Status"",
                ""ApprovalUrl"",
                ""RawCreateResponse""
            )
            VALUES
            (
                @OrderId,
                @Provider,
                @ProviderOrderId,
                @Currency,
                @Amount,
                @Status,
                @ApprovalUrl,
                @RawCreateResponse
            )
            ON CONFLICT (""OrderId"", ""Provider"") DO UPDATE SET
                ""ProviderOrderId"" = EXCLUDED.""ProviderOrderId"",
                ""Currency"" = EXCLUDED.""Currency"",
                ""Amount"" = EXCLUDED.""Amount"",
                ""Status"" = EXCLUDED.""Status"",
                ""ApprovalUrl"" = EXCLUDED.""ApprovalUrl"",
                ""RawCreateResponse"" = EXCLUDED.""RawCreateResponse"",
                ""UpdateDate"" = NOW();";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            request.OrderId,
            Provider,
            request.ProviderOrderId,
            request.Currency,
            request.Amount,
            request.Status,
            request.ApprovalUrl,
            request.RawCreateResponse
        }, cancellationToken: cancellationToken));
    }

    public async Task MarkPaymentCapturedAsync(CapturePaymentPersistenceRequest request, CancellationToken cancellationToken)
    {
        const string sqlPayment = @"
            UPDATE public.""OrderPayments""
            SET
                ""ProviderCaptureId"" = @ProviderCaptureId,
                ""Status"" = @Status,
                ""PayerEmail"" = @PayerEmail,
                ""RawCaptureResponse"" = @RawCaptureResponse,
                ""UpdateDate"" = NOW()
            WHERE ""Provider"" = @Provider AND ""ProviderOrderId"" = @ProviderOrderId;";

        const string sqlOrder = @"
            UPDATE public.""Orders""
            SET ""OrderState"" = @OrderState
            WHERE ""OrderId"" = @OrderId;";

        await using var connection = (NpgsqlConnection)_context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(sqlPayment, new
            {
                Provider,
                request.ProviderOrderId,
                request.ProviderCaptureId,
                request.Status,
                request.PayerEmail,
                request.RawCaptureResponse
            }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(sqlOrder, new
            {
                request.OrderId,
                request.OrderState
            }, transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error marking captured PayPal payment for order {OrderId}", request.OrderId);
            throw;
        }
    }

    public async Task MarkPaymentFailedAsync(string paypalOrderId, string status, string? rawCaptureResponse, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE public.""OrderPayments""
            SET
                ""Status"" = @Status,
                ""RawCaptureResponse"" = @RawCaptureResponse,
                ""UpdateDate"" = NOW()
            WHERE ""Provider"" = @Provider AND ""ProviderOrderId"" = @PayPalOrderId;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Provider,
            Status = status,
            RawCaptureResponse = rawCaptureResponse,
            PayPalOrderId = paypalOrderId
        }, cancellationToken: cancellationToken));
    }

    public async Task LogWebhookEventAsync(PayPalWebhookEventLogEntry entry, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO public.""PayPalWebhookEvents""
            (
                ""EventId"",
                ""EventType"",
                ""VerificationStatus"",
                ""ProviderOrderId"",
                ""Payload"",
                ""Headers""
            )
            VALUES
            (
                @EventId,
                @EventType,
                @VerificationStatus,
                @ProviderOrderId,
                @Payload,
                @Headers
            )
            ON CONFLICT (""EventId"") DO NOTHING;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, entry, cancellationToken: cancellationToken));
    }
}
