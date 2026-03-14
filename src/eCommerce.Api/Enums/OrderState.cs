namespace eCommerce.Api.Enums;

public enum OrderState
{
    CANCELLED = 0,
    CONFIRMED = 1,
    PENDING_PAYMENT = 2,
    PAID = 3,
    PAYMENT_FAILED = 4
}
