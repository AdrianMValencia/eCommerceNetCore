using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Entities;
using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Features.Products;

public class GetAllProducts
{
    #region Query
    public sealed class Query : IQuery<IEnumerable<Product>> { }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : IQueryHandler<Query, IEnumerable<Product>>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<IEnumerable<Product>>> Handle(Query query, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetProductsAsync(cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<IEnumerable<Product>>> GetProductsAsync(CancellationToken cancellationToken)
        {
            var response = new BaseResponse<IEnumerable<Product>>();

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
                ORDER BY ""Id"";";

            try
            {
                using var connection = _context.CreateConnection();

                var result = await connection.QueryAsync<Product>(sql);

                response.IsSuccess = true;
                response.Data = result.ToList();
                response.Message = "Productos obtenidos correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al obtener los productos. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetAllProductsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/products", async (
                IDispatcher dispatcher,
                CancellationToken cancellationToken
            ) =>
            {
                var response = await dispatcher.Dispatch<Query, IEnumerable<Product>>(new Query(), cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
