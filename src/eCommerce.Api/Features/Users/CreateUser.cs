using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Enums;
using eCommerce.Api.Shared.Bases;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.Api.Features.Users;

public class CreateUser
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string Email { get; set; } = null!;
        public string? Address { get; set; }
        public string? Cellphone { get; set; }
        public string? UserType { get; set; }
    }
    #endregion

    #region Validator 
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

            //RuleFor(x => x.Password)
            //    .NotEmpty().WithMessage("Password is required.")
            //    .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            //    .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            //    .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            //    .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            //    .Matches(@"[\W]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.Firstname)
                .NotEmpty().WithMessage("Firstname is required.")
                .NotNull().WithMessage("Firstname cannot be null.")
                .MaximumLength(50).WithMessage("Firstname cannot exceed 50 characters.");

            RuleFor(x => x.Lastname)
                .NotEmpty().WithMessage("Lastname is required.")
                .NotNull().WithMessage("Lastname cannot be null.")
                .MaximumLength(50).WithMessage("Lastname cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .NotNull().WithMessage("Email cannot be null.")
                .EmailAddress().WithMessage("Email must be a valid email address.");
        }
    }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : ICommandHandler<Command, bool>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<bool>> Handle(Command command, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                command,
                async () => await CreateUserAsync(command, cancellationToken),
                cancellationToken
                );
        }

        private async Task<BaseResponse<bool>> CreateUserAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            const string sql = @"
                INSERT INTO public.""Users""
                    (
                        ""Username"", 
                        ""Password"", 
                        ""Firstname"", 
                        ""Lastname"", 
                        ""Email"", 
                        ""Address"", 
                        ""Cellphone"", 
                        ""UserType"", 
                        ""CreateDate""
                    )
                VALUES 
                    (
                        @Username, 
                        @Password, 
                        @Firstname, 
                        @Lastname, 
                        @Email, 
                        @Address, 
                        @Cellphone, 
                        @UserType, 
                        NOW()
                    );";

            try
            {
                using var connection = _context.CreateConnection();

                // La contraseña nunca se persiste en texto plano.
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(command.Password);

                var parameters = new DynamicParameters();
                parameters.Add("Username", command.Username);
                parameters.Add("Password", hashedPassword);
                parameters.Add("Lastname", command.Lastname);
                parameters.Add("Firstname", command.Firstname);
                parameters.Add("Email", command.Email);
                parameters.Add("Address", command.Address);
                parameters.Add("Cellphone", command.Cellphone);
                parameters.Add("UserType", command.UserType);

                var result = await connection.ExecuteAsync(sql, parameters);

                response.IsSuccess = true;
                response.Data = result > 0;
                response.Message = "Se registró correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al registrar el usuario. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class CreateUserEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users", async (
                Command command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken
                ) =>
            {
                var response = await dispatcher.Dispatch<Command, bool>(command, cancellationToken);
                return Results.Ok(response);
            })
            .AllowAnonymous();
        }
    }
    #endregion
}
