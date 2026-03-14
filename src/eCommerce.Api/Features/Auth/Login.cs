using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Entities;
using eCommerce.Api.Services.Auth;
using eCommerce.Api.Shared.Bases;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.Api.Features.Auth;

public class Login
{
    #region Command
    /// <summary>
    /// Request de autenticación. Se autentica por correo y contraseña.
    /// </summary>
    public sealed class Command : ICommand<Response>
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    /// <summary>
    /// Respuesta del login con token, expiración y roles del usuario.
    /// </summary>
    public sealed class Response
    {
        public string Token { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresInMinutes { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserType { get; set; } = null!;
        public List<string> Roles { get; set; } = [];
    }
    #endregion

    #region Validator
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
    #endregion

    #region Handler
    internal sealed class Handler(
        ApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IConfiguration configuration,
        HandlerExecutor executor) : ICommandHandler<Command, Response>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;
        private readonly IConfiguration _configuration = configuration;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                command,
                async () => await LoginAsync(command, cancellationToken),
                cancellationToken);
        }

        private async Task<BaseResponse<Response>> LoginAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<Response>();

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
                WHERE ""Email"" = @Email;";

            try
            {
                using var connection = _context.CreateConnection();
                var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { command.Email });

                if (user is null)
                {
                    response.IsSuccess = false;
                    response.Message = "Credenciales inválidas.";
                    return response;
                }

                // Se compara la contraseña ingresada con el hash almacenado en base de datos.
                var isPasswordValid = BCrypt.Net.BCrypt.Verify(command.Password, user.Password);
                if (!isPasswordValid)
                {
                    response.IsSuccess = false;
                    response.Message = "Credenciales inválidas.";
                    return response;
                }

                var token = _jwtTokenGenerator.GenerateToken(user);
                var expirationMinutes = _configuration.GetValue<int?>("Jwt:ExpirationMinutes") ?? 60;

                response.IsSuccess = true;
                response.Data = new Response
                {
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresInMinutes = expirationMinutes,
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    UserType = user.UserType.ToString(),
                    Roles = [user.UserType.ToString()]
                };
                response.Message = "Login exitoso.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al autenticar al usuario. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/login", async (
                Command command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var response = await dispatcher.Dispatch<Command, Response>(command, cancellationToken);
                return response.IsSuccess ? Results.Ok(response) : Results.BadRequest(response);
            })
            .AllowAnonymous();
        }
    }
    #endregion
}
