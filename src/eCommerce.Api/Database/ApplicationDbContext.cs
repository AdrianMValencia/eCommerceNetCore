using eCommerce.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Reflection;

namespace eCommerce.Api.Database;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
    IConfiguration configuration) : DbContext(options)
{
    private readonly IConfiguration _configuration = configuration;

    #region Entities
    public DbSet<User> Users { get; set; }
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public IDbConnection CreateConnection() 
        => new NpgsqlConnection(_configuration.GetConnectionString("EcommerceConnection"));
}
