using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Contracts.Users;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Bases;
using FluentValidation;

namespace eCommerce.Api.Features.Users;

public class GetUserByEmail
{
    #region Query
    public sealed class Query : IQuery<UserResponse>
    {
        public string Email { get; set; } = null!;
    }
    #endregion

    #region Validator
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .NotNull().WithMessage("Email cannot be null.")
                .EmailAddress().WithMessage("Email must be a valid email address.");
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

        public async Task<BaseResponse<UserResponse>> Handle(
            Query query, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetUserByEmailAsync(query, cancellationToken),
                cancellationToken
                );
        }

        private async Task<BaseResponse<UserResponse>> GetUserByEmailAsync(
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
                WHERE ""Email"" = @Email";

            try
            {
                using var connection = _context.CreateConnection();

                var user = await connection
                    .QueryFirstOrDefaultAsync<UserResponse>(sql, new { query.Email });

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
    public class GetUserByEmailEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/users/by-email/{email}", async (
                string email,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var query = new Query { Email = email };
                var response = await dispatcher
                    .Dispatch<Query, UserResponse>(query, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
