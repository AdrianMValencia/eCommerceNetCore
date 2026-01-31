using eCommerce.Api.Enums;

namespace eCommerce.Api.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string? Cellphone { get; set; }
    public UserType UserType { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
