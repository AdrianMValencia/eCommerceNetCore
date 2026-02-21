using eCommerce.Api.Enums;

namespace eCommerce.Api.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderState OrderState { get; set; }
    public User? User { get; set; }
    public List<OrderDetail>? OrderDetails { get; set; }
    public decimal Total { get; set; }
}
