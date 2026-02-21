using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Bases;
using FluentValidation;

namespace eCommerce.Api.Features.Categories;

public class DeleteCategory
{
    #region Command
    public sealed class Command : ICommand<bool>
    {
        public int Id { get; set; }
    }
    #endregion

    #region Validator
    public class Validator : AbstractValidator<Command>
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
        HandlerExecutor executor) : ICommandHandler<Command, bool>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<bool>> Handle(Command command, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                command,
                async () => await DeleteCategoryAsync(command, cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<bool>> DeleteCategoryAsync(Command command, CancellationToken cancellationToken)
        {
            var response = new BaseResponse<bool>();

            const string sql = @"
                DELETE FROM public.""Categories""
                WHERE ""Id"" = @Id;";

            try
            {
                using var connection = _context.CreateConnection();

                var result = await connection.ExecuteAsync(sql, new { command.Id });

                response.IsSuccess = true;
                response.Data = result > 0;
                response.Message = result > 0 ? "Se eliminó correctamente." : "Categoría no encontrada.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al eliminar la categoría. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class DeleteCategoryEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/categories/{id:int}", async (
                int id,
                IDispatcher dispatcher,
                CancellationToken cancellationToken
            ) =>
            {
                var command = new Command { Id = id };
                var response = await dispatcher.Dispatch<Command, bool>(command, cancellationToken);
                return Results.Ok(response);
            });
        }
    }
    #endregion
}
