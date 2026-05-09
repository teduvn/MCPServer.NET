using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders.Queries
{
    /// <summary>
    /// Query lấy chi tiết một Order theo Id.
    /// </summary>
    public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDto?>>;

    /// <summary>
    /// Handler xử lý GetOrderByIdQuery.
    /// Demo cách sử dụng ToDto() extension method.
    /// </summary>
    public sealed class GetOrderByIdQueryHandler(IApplicationDbContext _context)
        : IRequestHandler<GetOrderByIdQuery, Result<OrderDto?>>
    {
        public async Task<Result<OrderDto?>> Handle(
            GetOrderByIdQuery request,
            CancellationToken ct)
        {
            // Dùng projection — KHÔNG gọi FirstOrDefaultAsync() rồi map sau
            var dto = await _context.Orders
                .AsNoTracking()                          // Không track change — readonly
                .Where(o => o.Id == request.OrderId)
                .Select(o => new OrderDto              // Projection ngay trong query
                {
                    Id = o.Id,
                    TotalAmount = o.TotalAmount.Amount,
                    Currency = o.TotalAmount.Currency,
                    Status = o.Status.ToString(),
                    Items = o.Items.Select(i => new OrderItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice.Amount,
                        Subtotal = i.UnitPrice.Amount * i.Quantity
                    }).ToList()
                })
                .FirstOrDefaultAsync(ct);


                if (dto is null)
                    return Result<OrderDto?>.Failure(OrderErrors.NotFound);


                return Result<OrderDto?>.Success(dto);

        }
    }
}
