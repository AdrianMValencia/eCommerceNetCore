using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Bases;
using FluentValidation;

namespace eCommerce.Api.Features.Categories;

public class CreateCategory
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public string Name { get; set; } = null!;
    }
    #endregion

    #region Validator
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .NotNull().WithMessage("Name cannot be null.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
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
                async () => await CreateCategoryAsync(command, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<bool>> CreateCategoryAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            const string sql = @"
                INSERT INTO public.""Categories""
                    (
                        ""Name"",
                        ""CreateDate""
                    )
                VALUES
                    (
                        @Name,
                        NOW()
                    );";

            try
            {
                using var connection = _context.CreateConnection();

                var result = await connection.ExecuteAsync(sql, new { command.Name });

                response.IsSuccess = true;
                response.Data = result > 0;
                response.Message = "Se registró correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al registrar la categoría. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class CreateCategoryEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/categories", async (
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
