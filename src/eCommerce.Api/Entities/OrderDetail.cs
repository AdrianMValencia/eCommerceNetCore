namespace eCommerce.Api.Entities;

public class OrderDetail
{
    public int Id { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public int ProductId { get; set; }
    public Order? Order { get; set; }
}
