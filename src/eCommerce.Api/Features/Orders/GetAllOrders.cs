using Carter;
using Dapper;
using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Bases;
using eCommerce.Api.Shared.Security;

namespace eCommerce.Api.Features.Orders;

public class GetAllOrders
{
    #region Query
    public sealed class Query : IQuery<IEnumerable<OrderResponse>> { }
    #endregion

    #region Response Models
    public sealed class OrderResponse
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderState { get; set; } = null!;
        public decimal Total { get; set; }
        public OrderUserResponse? User { get; set; }
        public List<OrderItemResponse> Items { get; set; } = [];
    }

    public sealed class OrderUserResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string Email { get; set; } = null!;
    }

    public sealed class OrderItemResponse
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public ProductSummaryResponse? Product { get; set; }
    }

    public sealed class ProductSummaryResponse
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? Description { get; set; }
    }

    private sealed class OrderRow
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderState { get; set; } = null!;
        public decimal Total { get; set; }

        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? Email { get; set; }

        public int? OrderDetailId { get; set; }
        public int? ProductId { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }

        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductDescription { get; set; }
    }
    #endregion

    #region Handler
    internal sealed class Handler(ApplicationDbContext context,
        HandlerExecutor executor) : IQueryHandler<Query, IEnumerable<OrderResponse>>
    {
        private readonly ApplicationDbContext _context = context;
        private readonly HandlerExecutor _executor = executor;

        public async Task<BaseResponse<IEnumerable<OrderResponse>>> Handle(Query query, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                query,
                async () => await GetOrdersAsync(cancellationToken),
                cancellationToken
            );
        }

        private async Task<BaseResponse<IEnumerable<OrderResponse>>> GetOrdersAsync(CancellationToken cancellationToken)
        {
            var response = new BaseResponse<IEnumerable<OrderResponse>>();

            const string sql = @"
                SELECT
                    o.""OrderId"",
                    o.""OrderDate"",
                    o.""OrderState"",
                    o.""Total"",
                    u.""UserId"",
                    u.""Username"",
                    u.""Firstname"",
                    u.""Lastname"",
                    u.""Email"",
                    od.""OrderDetailId"",
                    od.""ProductId"",
                    od.""Quantity"",
                    od.""Price"",
                    p.""Name"" AS ""ProductName"",
                    p.""Code"" AS ""ProductCode"",
                    p.""Description"" AS ""ProductDescription""
                FROM public.""Orders"" o
                LEFT JOIN public.""Users"" u ON u.""UserId"" = o.""UserId""
                LEFT JOIN public.""OrderDetails"" od ON od.""OrderId"" = o.""OrderId""
                LEFT JOIN public.""Products"" p ON p.""ProductId"" = od.""ProductId""
                ORDER BY o.""OrderId"", od.""OrderDetailId"";";

            try
            {
                using var connection = _context.CreateConnection();

                var rows = await connection.QueryAsync<OrderRow>(sql);

                var orders = rows
                    .GroupBy(x => x.OrderId)
                    .Select(group =>
                    {
                        var first = group.First();

                        return new OrderResponse
                        {
                            OrderId = first.OrderId,
                            OrderDate = first.OrderDate,
                            OrderState = first.OrderState,
                            Total = first.Total,
                            User = first.UserId.HasValue
                                ? new OrderUserResponse
                                {
                                    UserId = first.UserId.Value,
                                    Username = first.Username ?? string.Empty,
                                    Firstname = first.Firstname,
                                    Lastname = first.Lastname,
                                    Email = first.Email ?? string.Empty
                                }
                                : null,
                            Items = group
                                .Where(x => x.OrderDetailId.HasValue)
                                .Select(x => new OrderItemResponse
                                {
                                    OrderDetailId = x.OrderDetailId!.Value,
                                    ProductId = x.ProductId ?? 0,
                                    Quantity = x.Quantity ?? 0,
                                    Price = x.Price ?? 0,
                                    Product = x.ProductId.HasValue
                                        ? new ProductSummaryResponse
                                        {
                                            Name = x.ProductName ?? string.Empty,
                                            Code = x.ProductCode ?? string.Empty,
                                            Description = x.ProductDescription
                                        }
                                        : null
                                })
                                .ToList()
                        };
                    })
                    .ToList();

                response.IsSuccess = true;
                response.Data = orders;
                response.Message = "Órdenes obtenidas correctamente.";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = $"Ocurrió un error al obtener las órdenes. {ex.Message}";
            }

            return response;
        }
    }
    #endregion

    #region Endpoint
    public class GetAllOrdersEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/orders", async (
                IDispatcher dispatcher,
                CancellationToken cancellationToken
            ) =>
            {
                var response = await dispatcher.Dispatch<Query, IEnumerable<OrderResponse>>(new Query(), cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization(AuthPolicies.AdminAccess);
        }
    }
    #endregion
}
