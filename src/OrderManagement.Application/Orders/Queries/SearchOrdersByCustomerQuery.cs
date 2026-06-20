using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Orders.Queries
{
    public sealed record SearchOrdersByCustomerQuery(
        string Query,
        int Page,
        int PageSize) : IRequest<Result<PagedResult<OrderSummaryDto>>>;

    public sealed class SearchOrdersByCustomerQueryHandler(IApplicationDbContext context)
        : IRequestHandler<SearchOrdersByCustomerQuery, Result<PagedResult<OrderSummaryDto>>>
    {
        public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(
            SearchOrdersByCustomerQuery request,
            CancellationToken cancellationToken)
        {
            var pattern = $"%{request.Query.Trim()}%";
            var matchedCustomerIds = context.Customers
                .AsNoTracking()
                .Where(c =>
                    EF.Functions.Like(c.Email, pattern) ||
                    EF.Functions.Like(c.FirstName, pattern) ||
                    EF.Functions.Like(c.LastName, pattern) ||
                    EF.Functions.Like(c.FirstName + " " + c.LastName, pattern))
                .Select(c => c.Id);

            var ordersQuery = context.Orders
                .AsNoTracking()
                .Where(o =>
                    matchedCustomerIds.Contains(o.CustomerId) ||
                    EF.Functions.Like(o.CustomerEmail, pattern));

            var totalCount = await ordersQuery.CountAsync(cancellationToken);
            var items = await ordersQuery
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
