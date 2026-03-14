namespace eCommerce.Api.Options;

public sealed class PayPalOptions
{
    public const string SectionName = "PayPal";

    public string BaseUrl { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string WebhookId { get; set; } = null!;
    public string ReturnUrl { get; set; } = null!;
    public string CancelUrl { get; set; } = null!;
    public string Currency { get; set; } = "USD";
}
