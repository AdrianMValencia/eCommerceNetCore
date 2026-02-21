using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Bases;
using FluentValidation;

namespace eCommerce.Api.Features.Products;

public class CreateProduct
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Description { get; set; }
        public string? UrlImage { get; set; }
        public decimal Price { get; set; }
        public int UserId { get; set; }
        public int CategoryId { get; set; }
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
                .MaximumLength(150).WithMessage("Name cannot exceed 150 characters.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required.")
                .NotNull().WithMessage("Code cannot be null.")
                .MaximumLength(50).WithMessage("Code cannot exceed 50 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.");

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId must be greater than 0.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("CategoryId must be greater than 0.");
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
                async () => await CreateProductAsync(command, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<bool>> CreateProductAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            const string sql = @"
                INSERT INTO public.""Products""
                    (
                        ""Name"",
                        ""Code"",
                        ""Description"",
                        ""UrlImage"",
                        ""Price"",
                        ""CreatedDate"",
                        ""UserId"",
                        ""CategoryId""
                    )
                VALUES
                    (
                        @Name,
                        @Code,
                        @Description,
                        @UrlImage,
                        @Price,
                        NOW(),
                        @UserId,
                        @CategoryId
                    );";

            try
            {
                using var connection = _context.CreateConnection();

                var result = await connection.ExecuteAsync(sql, new
                {
                    command.Name,
                    command.Code,
                    command.Description,
                    command.UrlImage,
                    command.Price,
                    command.UserId,
                    command.CategoryId
                });

                response.IsSuccess = true;
                response.Data = result > 0;
                response.Message = "Se registró correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al registrar el producto. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class CreateProductEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/products", async (
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
