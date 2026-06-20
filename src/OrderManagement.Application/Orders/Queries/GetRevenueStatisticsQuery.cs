using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Queries
{
    public sealed record GetRevenueStatisticsQuery(
        DateTime FromDate,
        DateTime ToDate,
        string GroupBy) : IRequest<Result<IReadOnlyList<RevenueStatisticsGroupDto>>>;

    public sealed class GetRevenueStatisticsQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetRevenueStatisticsQuery, Result<IReadOnlyList<RevenueStatisticsGroupDto>>>
    {
        public async Task<Result<IReadOnlyList<RevenueStatisticsGroupDto>>> Handle(
            GetRevenueStatisticsQuery request,
            CancellationToken cancellationToken)
        {
            var baseQuery = context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.CreatedAt >= request.FromDate &&
                    o.CreatedAt <= request.ToDate &&
                    o.Status != OrderStatus.Cancelled);

            IReadOnlyList<RevenueStatisticsGroupDto> result = request.GroupBy switch
            {
                "currency" => await baseQuery
                    .GroupBy(o => o.Currency)
                    .Select(g => new RevenueStatisticsGroupDto
                    {
                        GroupKey = g.Key,
                        Currency = g.Key,
                        OrderCount = g.Count(),
                        TotalRevenue = g.Sum(x => x.TotalAmount.Amount),
                        AverageOrderValue = g.Average(x => x.TotalAmount.Amount)
                    })
                    .OrderBy(x => x.GroupKey)
                    .ToListAsync(cancellationToken),
                "month" => await baseQuery
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month, o.Currency })
                    .Select(g => new RevenueStatisticsGroupDto
                    {
                        GroupKey = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                        Currency = g.Key.Currency,
                        OrderCount = g.Count(),
                        TotalRevenue = g.Sum(x => x.TotalAmount.Amount),
                        AverageOrderValue = g.Average(x => x.TotalAmount.Amount)
                    })
                    .OrderBy(x => x.GroupKey)
                    .ThenBy(x => x.Currency)
                    .ToListAsync(cancellationToken),
                _ => await baseQuery
                    .GroupBy(o => new { Date = o.CreatedAt.Date, o.Currency })
                    .Select(g => new RevenueStatisticsGroupDto
                    {
                        GroupKey = g.Key.Date.ToString("yyyy-MM-dd"),
                        Currency = g.Key.Currency,
                        OrderCount = g.Count(),
                        TotalRevenue = g.Sum(x => x.TotalAmount.Amount),
                        AverageOrderValue = g.Average(x => x.TotalAmount.Amount)
                    })
                    .OrderBy(x => x.GroupKey)
                    .ThenBy(x => x.Currency)
                    .ToListAsync(cancellationToken)
            };

            return Result<IReadOnlyList<RevenueStatisticsGroupDto>>.Success(result);
        }
    }
}
