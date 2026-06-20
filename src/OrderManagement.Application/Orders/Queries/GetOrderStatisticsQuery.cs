using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Queries
{
    public sealed record GetOrderStatisticsQuery(DateTime FromDate, DateTime ToDate)
        : IRequest<Result<OrderStatisticsDto>>;

    public sealed class GetOrderStatisticsQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetOrderStatisticsQuery, Result<OrderStatisticsDto>>
    {
        public async Task<Result<OrderStatisticsDto>> Handle(
            GetOrderStatisticsQuery request,
            CancellationToken cancellationToken)
        {
            var ordersQuery = context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= request.FromDate && o.CreatedAt <= request.ToDate);

            var totalOrders = await ordersQuery.CountAsync(cancellationToken);
            var totalItems = await ordersQuery.SumAsync(o => o.Items.Count, cancellationToken);
            var totalRevenue = await ordersQuery
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.TotalAmount.Amount, cancellationToken);
            var byStatus = await ordersQuery
                .GroupBy(o => o.Status)
                .Select(g => new StatusCountDto
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderBy(x => x.Status)
                .ToListAsync(cancellationToken);

            return Result<OrderStatisticsDto>.Success(new OrderStatisticsDto
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                TotalOrders = totalOrders,
                TotalItems = totalItems,
                TotalRevenueExcludingCancelled = totalRevenue,
                ByStatus = byStatus
            });
        }
    }
}
