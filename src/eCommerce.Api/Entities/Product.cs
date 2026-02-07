namespace eCommerce.Api.Entities;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public string? UrlImage { get; set; }
    public decimal Price { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
}
