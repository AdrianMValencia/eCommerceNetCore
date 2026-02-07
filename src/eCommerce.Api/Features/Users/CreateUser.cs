using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Enums;
using eCommerce.Api.Shared.Bases;
using FluentValidation;

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
        public UserType UserType { get; set; }
    }
    #endregion

    #region Validator 
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
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

                var parameters = new DynamicParameters();
                parameters.Add("Username", command.Username);
                parameters.Add("Password", command.Password);
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
            });
        }
    }
    #endregion
}
