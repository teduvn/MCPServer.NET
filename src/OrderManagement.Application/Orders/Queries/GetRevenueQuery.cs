using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Queries
{
    public sealed record GetRevenueQuery(int Month, int Year)
        : IRequest<Result<IReadOnlyList<RevenueStatisticsDto>>>;

    public sealed class GetRevenueQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetRevenueQuery, Result<IReadOnlyList<RevenueStatisticsDto>>>
    {
        public async Task<Result<IReadOnlyList<RevenueStatisticsDto>>> Handle(
            GetRevenueQuery request,
            CancellationToken cancellationToken)
        {
            var statistics = await context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.CreatedAt.Month == request.Month &&
                    o.CreatedAt.Year == request.Year &&
                    o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.Currency)
                .Select(g => new RevenueStatisticsDto
                {
                    Month = request.Month,
                    Year = request.Year,
                    Currency = g.Key,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(x => x.TotalAmount.Amount),
                    AverageOrderValue = g.Average(x => x.TotalAmount.Amount)
                })
                .OrderBy(x => x.Currency)
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<RevenueStatisticsDto>>.Success(statistics);
        }
    }
}
