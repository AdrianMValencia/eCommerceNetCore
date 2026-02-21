using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Entities;
using eCommerce.Api.Shared.Bases;
using FluentValidation;

namespace eCommerce.Api.Features.Products;

public class GetByIdProduct
{
    #region Query
    public sealed class Query : IQuery<Product?>
    {
        public int Id { get; set; }
    }
    #endregion

    #region Validator
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : IQueryHandler<Query, Product?>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<Product?>> Handle(Query query, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetProductAsync(query, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<Product?>> GetProductAsync(Query query, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<Product?>();

            const string sql = @"
                SELECT 
                    ""Id"",
                    ""Name"",
                    ""Code"",
                    ""Description"",
                    ""UrlImage"",
                    ""Price"",
                    ""CreatedDate"",
                    ""UpdatedDate"",
                    ""UserId"",
                    ""CategoryId""
                FROM public.""Products""
                WHERE ""Id"" = @Id;";

            try
            {
                using var connection = _context.CreateConnection();

                var product = await connection.QueryFirstOrDefaultAsync<Product>(sql, new { query.Id });

                response.IsSuccess = true;
                response.Data = product;
                response.Message = product is null ? "Producto no encontrado." : "Producto encontrado.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al obtener el producto. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetByIdProductEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/products/{id:int}", async (
                int id,
                IDispatcher dispatcher,
                CancellationToken cancellationToken
            ) =>
            {
                var query = new Query { Id = id };
                var response = await dispatcher.Dispatch<Query, Product?>(query, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
