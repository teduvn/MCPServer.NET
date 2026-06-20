namespace OrderManagement.Application.Orders.DTOs
{
    public sealed class OrderStatisticsDto
    {
        public DateTime FromDate { get; init; }
        public DateTime ToDate { get; init; }
        public int TotalOrders { get; init; }
        public int TotalItems { get; init; }
        public decimal TotalRevenueExcludingCancelled { get; init; }
        public IReadOnlyList<StatusCountDto> ByStatus { get; init; } = [];
    }

    public sealed class StatusCountDto
    {
        public string Status { get; init; } = string.Empty;
        public int Count { get; init; }
    }
}
