using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Entities;
using eCommerce.Api.Shared.Bases;

namespace eCommerce.Api.Features.Categories;

public class GetAllCategories
{
    #region Query
    public sealed class Query : IQuery<IEnumerable<Category>> { }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : IQueryHandler<Query, IEnumerable<Category>>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<IEnumerable<Category>>> Handle(Query query, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetCategoriesAsync(cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<IEnumerable<Category>>> GetCategoriesAsync(CancellationToken cancellationToken)
        {
            var response = new BaseResponse<IEnumerable<Category>>();

            const string sql = @"
                SELECT 
                    ""Id"",
                    ""Name"",
                    ""CreateDate"",
                    ""UpdateDate""
                FROM public.""Categories""
                ORDER BY ""Id"";";

            try
            {
                using var connection = _context.CreateConnection();

                var result = await connection.QueryAsync<Category>(sql);

                response.IsSuccess = true;
                response.Data = result.ToList();
                response.Message = "Categorías obtenidas correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al obtener las categorías. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetAllCategoriesEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/categories", async (
                IDispatcher dispatcher,
                CancellationToken cancellationToken
            ) =>
            {
                var response = await dispatcher.Dispatch<Query, IEnumerable<Category>>(new Query(), cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
