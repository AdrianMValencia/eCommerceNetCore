using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Contracts.Users;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Bases;
using FluentValidation;

namespace eCommerce.Api.Features.Users;

public class GetUserById
{
    #region Query
    public sealed class Query : IQuery<UserResponse>
    {
        public int UserId { get; set; }
    }
    #endregion

    #region Validator
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("El UserId debe ser mayor que cero.");
        }
    }
    #endregion

    #region Handler
    internal sealed class Handler(
        ApplicationDbContext context,
        HandlerExecutor executor) : IQueryHandler<Query, UserResponse>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<UserResponse>> Handle(Query query,
            CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetUserByIdAsync(query, cancellationToken),
                cancellationToken
                );
        }

        private async Task<BaseResponse<UserResponse>> GetUserByIdAsync(
            Query query,
            CancellationToken cancellationToken)
        {
            var response = new BaseResponse<UserResponse>();

            const string sql = @"
                SELECT 
                    ""UserId"",
                    ""Username"",
                    ""Password"",
                    ""Firstname"",
                    ""Lastname"",
                    ""Email"",
                    ""Address"",
                    ""Cellphone"",
                    ""UserType"",
                    ""CreateDate"",
                    ""UpdateDate""
                FROM public.""Users""
                WHERE ""UserId"" = @UserId";

            try
            {
                using var connection = _context.CreateConnection();

                var user = await connection
                    .QueryFirstOrDefaultAsync<UserResponse>(sql, new { query.UserId });

                response.IsSuccess = true;
                response.Data = user;
                response.Message = user is null ? "Usuario no encontrado."
                    : "Usuario obtenido exitosamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Error al obtener el usuario. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetUserByIdEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/{userId:int}", async (
                int userId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var query = new Query { UserId = userId };
                var response = await dispatcher
                    .Dispatch<Query, UserResponse>(query, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
