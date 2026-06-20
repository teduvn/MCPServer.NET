using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Queries
{
    public sealed record ListOrdersQuery(
        OrderStatus? Status,
        DateTime? FromDate,
        DateTime? ToDate,
        Guid? CustomerId,
        int Page,
        int PageSize) : IRequest<Result<PagedResult<OrderSummaryDto>>>;

    public sealed class ListOrdersQueryHandler(IApplicationDbContext context)
        : IRequestHandler<ListOrdersQuery, Result<PagedResult<OrderSummaryDto>>>
    {
        public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(
            ListOrdersQuery request,
            CancellationToken cancellationToken)
        {
            var query = context.Orders.AsNoTracking().AsQueryable();

            if (request.Status.HasValue)
            {
                query = query.Where(o => o.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
            }

            if (request.CustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == request.CustomerId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new OrderSummaryDto
                {
                    Id = o.Id,
                    OrderNumber = o.Id.ToString(),
                    OrderDate = o.CreatedAt,
                    CustomerId = o.CustomerId,
                    CustomerName = o.CustomerEmail,
                    TotalAmount = o.TotalAmount.Amount,
                    Currency = o.Currency,
                    Status = o.Status.ToString(),
                    ItemCount = o.Items.Count
                })
                .ToListAsync(cancellationToken);

            return Result<PagedResult<OrderSummaryDto>>.Success(
                new PagedResult<OrderSummaryDto>(items, totalCount, request.Page, request.PageSize));
        }
    }
}
