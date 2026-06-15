namespace OrderManagement.Application.Orders.DTOs
{
    public sealed class RevenueStatisticsDto
    {
        public int Month { get; init; }
        public int Year { get; init; }
        public string Currency { get; init; } = "VND";
        public int OrderCount { get; init; }
        public decimal TotalRevenue { get; init; }
        public decimal AverageOrderValue { get; init; }
    }
}
