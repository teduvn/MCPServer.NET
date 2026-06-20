using MediatR;
using ModelContextProtocol.Server;
using OrderManagement.Application.Common.Authorization;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.Commands.UpdateOrderStatus;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Queries;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.McpServer.Authorization;
using OrderManagement.McpServer.Models;
using System.ComponentModel;

namespace OrderManagement.McpServer.Tools;

[McpServerToolType]
public sealed class OrderContractTools
{
    private static readonly string[] MutableStatuses = ["Placed", "Shipped"];
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMcpContextAccessor _mcpContextAccessor;

    public OrderContractTools(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IMcpContextAccessor mcpContextAccessor)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _mcpContextAccessor = mcpContextAccessor;
    }

    [McpServerTool(Name = "list_orders")]
    [Description("Retrieve a paginated list of orders with optional filters for status, created date range, and customerId.")]
    public async Task<PagedToolResult<OrderSummaryRow>> ListOrders(
        [Description("Optional order status filter. Valid values: Draft, Placed, Confirmed, Shipped, Delivered, Cancelled.")]
        string? status = null,
        [Description("Optional UTC start date in ISO 8601 format, for example 2026-06-01 or 2026-06-01T00:00:00Z.")]
        string? fromDate = null,
        [Description("Optional UTC end date in ISO 8601 format, for example 2026-06-30 or 2026-06-30T23:59:59Z.")]
        string? toDate = null,
        [Description("Optional customer ID in GUID format.")]
        string? customerId = null,
        [Description("Page number to retrieve, 1-based. Default is 1.")]
        int page = 1,
        [Description("Number of items per page. Default is 10, maximum is 100.")]
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(status, out var parsedStatus, out var statusError))
        {
            throw new ArgumentException(statusError);
        }

        var result = await _mediator.Send(
            new ListOrdersQuery(
                parsedStatus,
                ParseOptionalUtcDate(fromDate, nameof(fromDate)),
                ParseOptionalUtcDate(toDate, nameof(toDate)),
                ParseOptionalGuid(customerId, nameof(customerId)),
                NormalizePage(page),
                NormalizePageSize(pageSize)),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Description);
        }

        return MapPagedResult(result.Value);
    }

    [McpServerTool(Name = "search_orders_by_customer")]
    [Description("Search orders by customer name or email with pagination. Use this when the caller knows a customer identifier string but not an order ID.")]
    public async Task<PagedToolResult<OrderSummaryRow>> SearchOrdersByCustomer(
        [Description("Customer name or email search string. Partial match is supported.")]
        string query,
        [Description("Page number to retrieve, 1-based. Default is 1.")]
        int page = 1,
        [Description("Number of items per page. Default is 10, maximum is 100.")]
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("query is required.", nameof(query));
        }

        var result = await _mediator.Send(
            new SearchOrdersByCustomerQuery(query, NormalizePage(page), NormalizePageSize(pageSize)),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Description);
        }

        return MapPagedResult(result.Value);
    }

    [McpServerTool(Name = "create_order")]
    [Description("Create a new order. Requires customerId and items. ShippingAddress remains optional for compatibility with the existing place order flow.")]
    public async Task<string> CreateOrder(
        [Description("Order payload. customerId and items are required.")]
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _mcpContextAccessor.SetTool("create_order");

        var customerId = ParseRequiredGuid(request.CustomerId, nameof(request.CustomerId));
        var shippingAddress = request.ShippingAddress is null
            ? await ResolveCustomerBillingAddress(customerId, cancellationToken)
            : new AddressDto
            {
                Street = request.ShippingAddress.Street,
                City = request.ShippingAddress.City,
                Province = request.ShippingAddress.Province,
                PostalCode = request.ShippingAddress.PostalCode,
                Country = request.ShippingAddress.Country,
                FormattedAddress = request.ShippingAddress.FormattedAddress
            };

        var command = new PlaceOrderCommand
        {
            CustomerId = customerId,
            ShippingAddress = shippingAddress
                ?? throw new ArgumentException("shippingAddress is required when the customer has no billingAddress on file."),
            Items = request.Items?.Select(i => new OrderItemDto
            {
                ProductId = ParseRequiredGuid(i.ProductId, nameof(i.ProductId)),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Currency = i.Currency,
                ProductName = string.Empty
            }).ToList() ?? []
        };

        if (command.Items.Count == 0)
        {
            throw new ArgumentException("items must contain at least one element.");
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsFailure
            ? $"Failed to create order: {result.Error.Description}"
            : $"Order created successfully. Order ID: {result.Value}";
    }

    [McpServerTool(Name = "update_order_status")]
    [RequiresClaim("permission", Permissions.Orders.Manage)]
    [Description("Update order status for supported workflow transitions. Cancelled is intentionally excluded; use cancel_order for cancellation with audit reason.")]
    public async Task<string> UpdateOrderStatus(
        [Description("Order ID in GUID format.")]
        string orderId,
        [Description("Target status. Supported values currently: Placed, Shipped.")]
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        AuthorizeAnyClaim(Permissions.Orders.Manage);
        _mcpContextAccessor.SetTool("update_order_status");

        var orderGuid = ParseRequiredGuid(orderId, nameof(orderId));
        if (!Enum.TryParse<OrderStatus>(newStatus, true, out var targetStatus))
        {
            return $"Invalid newStatus '{newStatus}'. Supported values: {string.Join(", ", MutableStatuses)}.";
        }

        if (targetStatus == OrderStatus.Cancelled)
        {
            return "Use cancel_order for cancellation so the reason is captured in audit logs.";
        }

        var result = await _mediator.Send(new UpdateOrderStatusCommand(orderGuid, targetStatus), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Code == "Order.NotFound"
                ? $"Order '{orderId}' was not found."
                : $"Failed to update order status: {result.Error.Description}";
        }

        return $"Order '{orderGuid}' status updated to '{result.Value}'.";
    }

    [McpServerTool(Name = "get_revenue_statistics")]
    [RequiresClaim("permission", Permissions.Reports.ViewRevenue, Permissions.Orders.Manage)]
    [Description("Get revenue statistics in a date range. groupBy supports day, month, or currency. Cancelled orders are excluded.")]
    public async Task<IReadOnlyList<RevenueStatisticsRow>> GetRevenueStatistics(
        [Description("UTC start date in ISO 8601 format.")]
        string fromDate,
        [Description("UTC end date in ISO 8601 format.")]
        string toDate,
        [Description("Optional grouping mode: day, month, or currency. Default is day.")]
        string? groupBy = "day",
        CancellationToken cancellationToken = default)
    {
        AuthorizeAnyClaim(Permissions.Reports.ViewRevenue, Permissions.Orders.Manage);

        var normalizedGroupBy = string.IsNullOrWhiteSpace(groupBy) ? "day" : groupBy.Trim().ToLowerInvariant();
        var result = await _mediator.Send(
            new GetRevenueStatisticsQuery(
                ParseRequiredUtcDate(fromDate, nameof(fromDate)),
                ParseRequiredUtcDate(toDate, nameof(toDate)),
                normalizedGroupBy),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Description);
        }

        return result.Value.Select(x => new RevenueStatisticsRow
        {
            GroupKey = x.GroupKey,
            Currency = x.Currency,
            OrderCount = x.OrderCount,
            TotalRevenue = x.TotalRevenue,
            AverageOrderValue = x.AverageOrderValue
        }).ToList();
    }

    [McpServerTool(Name = "get_order_statistics")]
    [Description("Get order statistics for a date range, including total orders, total items, total revenue, and counts by status.")]
    public async Task<OrderStatisticsRow> GetOrderStatistics(
        [Description("UTC start date in ISO 8601 format.")]
        string fromDate,
        [Description("UTC end date in ISO 8601 format.")]
        string toDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetOrderStatisticsQuery(
                ParseRequiredUtcDate(fromDate, nameof(fromDate)),
                ParseRequiredUtcDate(toDate, nameof(toDate))),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Description);
        }

        return new OrderStatisticsRow
        {
            FromDate = result.Value.FromDate,
            ToDate = result.Value.ToDate,
            TotalOrders = result.Value.TotalOrders,
            TotalItems = result.Value.TotalItems,
            TotalRevenueExcludingCancelled = result.Value.TotalRevenueExcludingCancelled,
            ByStatus = result.Value.ByStatus
                .Select(x => new StatusCountRow
                {
                    Status = x.Status,
                    Count = x.Count
                })
                .ToList()
        };
    }

    [McpServerTool(Name = "bulk_update_order_status")]
    [RequiresClaim("permission", Permissions.Orders.Manage)]
    [Description("Bulk update order status for supported workflow transitions. Cancelled is intentionally excluded; use cancel_order per order when audit reason matters.")]
    public async Task<BulkUpdateResultRow> BulkUpdateOrderStatus(
        [Description("List of order IDs in GUID format.")]
        string[] orderIds,
        [Description("Target status. Supported values currently: Placed, Shipped.")]
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        AuthorizeAnyClaim(Permissions.Orders.Manage);
        _mcpContextAccessor.SetTool("bulk_update_order_status");

        if (orderIds is null || orderIds.Length == 0)
        {
            throw new ArgumentException("orderIds must contain at least one value.", nameof(orderIds));
        }

        var results = new List<BulkUpdateItemResultRow>();
        foreach (var orderId in orderIds)
        {
            try
            {
                var message = await UpdateOrderStatus(orderId, newStatus, cancellationToken);
                results.Add(new BulkUpdateItemResultRow
                {
                    OrderId = orderId,
                    Success = message.Contains("status updated", StringComparison.OrdinalIgnoreCase),
                    Message = message
                });
            }
            catch (Exception ex)
            {
                results.Add(new BulkUpdateItemResultRow
                {
                    OrderId = orderId,
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        return new BulkUpdateResultRow
        {
            NewStatus = newStatus,
            TotalRequested = orderIds.Length,
            Succeeded = results.Count(x => x.Success),
            Failed = results.Count(x => !x.Success),
            Results = results
        };
    }

    [McpServerTool(Name = "get_product")]
    [Description("Get product details by product ID.")]
    public async Task<ProductDetailRow?> GetProduct(
        [Description("Product ID in GUID format.")]
        string productId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetProductByIdQuery(ParseRequiredGuid(productId, nameof(productId))),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Description);
        }

        return result.Value is null
            ? null
            : new ProductDetailRow
            {
                Id = result.Value.Id,
                Name = result.Value.Name,
                Description = result.Value.Description,
                Price = result.Value.Price,
                Currency = result.Value.Currency,
                WeightKg = result.Value.WeightKg,
                StockQuantity = result.Value.StockQuantity,
                IsActive = result.Value.IsActive,
                CreatedAt = result.Value.CreatedAt,
                UpdatedAt = result.Value.UpdatedAt
            };
    }

    private async Task<AddressDto?> ResolveCustomerBillingAddress(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerBillingAddressQuery(customerId), cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Description);
        }

        return result.Value;
    }

    private PagedToolResult<OrderSummaryRow> MapPagedResult(PagedResult<OrderSummaryDto> source)
    {
        return new PagedToolResult<OrderSummaryRow>
        {
            Page = source.Page,
            PageSize = source.PageSize,
            TotalCount = source.TotalCount,
            Items = source.Items.Select(x => new OrderSummaryRow
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                OrderDate = x.OrderDate,
                CustomerId = x.CustomerId,
                CustomerEmail = x.CustomerName,
                TotalAmount = x.TotalAmount,
                Currency = x.Currency,
                Status = x.Status,
                ItemCount = x.ItemCount
            }).ToList()
        };
    }

    private void AuthorizeAnyClaim(params string[] permissions)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User context not found.");
        }

        if (!permissions.Any(permission => _currentUserService.HasClaim("permission", permission)))
        {
            throw new UnauthorizedAccessException(
                $"Access denied. Required permission(s): {string.Join(" or ", permissions)}.");
        }
    }

    private static bool TryParseStatus(string? status, out OrderStatus? parsedStatus, out string error)
    {
        parsedStatus = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (Enum.TryParse<OrderStatus>(status, true, out var parsed))
        {
            parsedStatus = parsed;
            return true;
        }

        error = $"Invalid status '{status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}.";
        return false;
    }

    private static int NormalizePage(int page) => page < 1 ? 1 : page;

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize < 1)
        {
            return 10;
        }

        return pageSize > 100 ? 100 : pageSize;
    }

    private static Guid ParseRequiredGuid(string value, string parameterName)
    {
        if (!Guid.TryParse(value, out var parsed))
        {
            throw new ArgumentException($"{parameterName} must be a valid GUID.", parameterName);
        }

        return parsed;
    }

    private static Guid? ParseOptionalGuid(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseRequiredGuid(value, parameterName);
    }

    private static DateTime ParseRequiredUtcDate(string value, string parameterName)
    {
        if (!DateTime.TryParse(value, out var parsed))
        {
            throw new ArgumentException($"{parameterName} must be a valid ISO 8601 date.", parameterName);
        }

        return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
    }

    private static DateTime? ParseOptionalUtcDate(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseRequiredUtcDate(value, parameterName);
    }
}
