using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Entities;
using eCommerce.Api.Shared.Bases;
using FluentValidation;

namespace eCommerce.Api.Features.Categories;

public class GetByIdCategory
{
    #region Query
    public sealed class Query : IQuery<Category?>
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
        HandlerExecutor executor) : IQueryHandler<Query, Category?>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<Category?>> Handle(Query query, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetCategoryAsync(query, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<Category?>> GetCategoryAsync(Query query, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<Category?>();

            const string sql = @"
                SELECT 
                    ""Id"",
                    ""Name"",
                    ""CreateDate"",
                    ""UpdateDate""
                FROM public.""Categories""
                WHERE ""Id"" = @Id;";

            try
            {
                using var connection = _context.CreateConnection();

                var category = await connection.QueryFirstOrDefaultAsync<Category>(sql, new { query.Id });

                response.IsSuccess = true;
                response.Data = category;
                response.Message = category is null ? "Categoría no encontrada." : "Categoría encontrada.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al obtener la categoría. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetByIdCategoryEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/categories/{id:int}", async (
                int id,
                IDispatcher dispatcher,
                CancellationToken cancellationToken
            ) =>
            {
                var query = new Query { Id = id };
                var response = await dispatcher.Dispatch<Query, Category?>(query, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
