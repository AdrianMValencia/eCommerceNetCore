namespace eCommerce.Api.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
