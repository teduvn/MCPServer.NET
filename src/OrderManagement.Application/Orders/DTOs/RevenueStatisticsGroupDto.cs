namespace OrderManagement.Application.Orders.DTOs
{
    public sealed class RevenueStatisticsGroupDto
    {
        public string GroupKey { get; init; } = string.Empty;
        public string Currency { get; init; } = string.Empty;
        public int OrderCount { get; init; }
        public decimal TotalRevenue { get; init; }
        public decimal AverageOrderValue { get; init; }
    }
}
