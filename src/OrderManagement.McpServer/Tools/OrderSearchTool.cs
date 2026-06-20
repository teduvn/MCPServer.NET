using MediatR;
using ModelContextProtocol.Server;
using OrderManagement.Application.Orders.Queries;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Specifications.Orders.Dtos;
using System.ComponentModel;

namespace OrderManagement.McpServer.Tools
{
    [McpServerToolType]
    public class OrderSearchTool
    {
        private readonly IMediator _mediator;
        private static readonly string[] ValidStatuses =
            Enum.GetNames(typeof(OrderStatus));


        public OrderSearchTool(IMediator mediator) => _mediator = mediator;


        [McpServerTool, Description("Search orders with flexible filters. All parameters optional. Returns id, customerEmail, status, totalAmount, currency, itemCount, and createdAt.")]
        public async Task<string> SearchOrders(
            [Description("Order status: Draft/Placed/Confirmed/Shipped/Delivered/Cancelled")]
        string? status = null,
            [Description("Customer email partial match (case-insensitive)")]
        string? customerName = null,
            [Description("Minimum total amount in VND")]
        decimal? minAmount = null,
            [Description("Maximum total amount in VND")]
        decimal? maxAmount = null,
            [Description("From date ISO 8601: 2024-01-15")]
        string? fromDate = null,
            [Description("To date ISO 8601: 2024-01-31")]
        string? toDate = null,
            [Description("Max results (default 20, max 100)")]
        int pageSize = 20)
        {
            OrderStatus? parsedStatus = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var parsed))
                    return $"Invalid status '{status}'. Valid statuses: {string.Join(", ", ValidStatuses)}.";

                parsedStatus = parsed;
            }


            var filter = new OrderSearchFilter
            {
                Status = parsedStatus,
                CustomerName = customerName,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                FromDate = fromDate != null ? DateTime.Parse(fromDate) : null,
                ToDate = toDate != null ? DateTime.Parse(toDate) : null,
                PageSize = Math.Min(pageSize, 100)
            };


            var result = await _mediator.Send(new SearchOrdersQuery(filter));


            if (!result.Any())
                return "No orders found matching the given criteria.";


            var lines = result.Select(o =>
                $"[{o.Id}] {o.CustomerName} | {o.Status} | {o.TotalAmount:N0} {o.Currency} | items: {o.ItemCount} | createdAt: {o.OrderDate:dd/MM/yyyy}"
            );
            return $"Found {result.Count} order(s):\n" + string.Join("\n", lines);
        }
    }

}
