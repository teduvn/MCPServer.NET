namespace OrderManagement.McpServer.Models;

public sealed class PagedToolResult<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<T> Items { get; init; } = [];
}

public sealed class OrderSummaryRow
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ItemCount { get; init; }
}

public sealed class RevenueStatisticsRow
{
    public string GroupKey { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageOrderValue { get; init; }
}

public sealed class OrderStatisticsRow
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public int TotalOrders { get; init; }
    public int TotalItems { get; init; }
    public decimal TotalRevenueExcludingCancelled { get; init; }
    public IReadOnlyList<StatusCountRow> ByStatus { get; init; } = [];
}

public sealed class StatusCountRow
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class BulkUpdateResultRow
{
    public string NewStatus { get; init; } = string.Empty;
    public int TotalRequested { get; init; }
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<BulkUpdateItemResultRow> Results { get; init; } = [];
}

public sealed class BulkUpdateItemResultRow
{
    public string OrderId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ProductDetailRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
