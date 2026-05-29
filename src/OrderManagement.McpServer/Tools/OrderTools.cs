using MediatR;
using ModelContextProtocol.Server;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Application.Common.Observability;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Queries;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Orders;
using OrderManagement.McpServer.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace OrderManagement.McpServer.Tools
{
    [McpServerToolType]  // đánh dấu class này chứa MCP tools
    public class OrderTools
    {
        private readonly IMediator _mediator;

        public OrderTools(IMediator mediator)
        {
            _mediator = mediator;
        }


        [McpServerTool(Name = "get_order")]
        [Description(
            "Retrieve a single order by its unique identifier. " +
            "Returns order details including status, customer info, line items, and total. " +
            "Use this when you need full details of a specific order. " +
            "For searching multiple orders, use list_orders or search_orders instead.")]
        public async Task<OrderDto?> GetOrder(
            [Description(
                "The order ID in GUID format (e.g. '3fa85f64-5717-4562-b3fc-2c963f66afa6'). " +
                "Obtain this from list_orders or search_orders first if you don't have it.")]
            Guid orderId)
        {
            // Span này là parent của span MediatR sẽ tạo
            using var activity = OmsActivitySource.Instance
                .StartActivity("McpTool: get_order");
            activity?.SetTag("mcp.tool", "get_order");
            activity?.SetTag("order.id", orderId.ToString());
            var sw = Stopwatch.StartNew();

            try
            {
                var query = new GetOrderByIdQuery(orderId);
                var result = await _mediator.Send(query);

                sw.Stop();

                if (result.IsFailure)
                {
                    OmsMeter.ToolLatency.Record(sw.ElapsedMilliseconds,new("tool", "get_order"), new("status", "error"));
                    OmsMeter.ToolCallCount.Add(1, new("tool", "get_order"), new("status", "error"));
                    return null;
                }

                OmsMeter.ToolLatency.Record(sw.ElapsedMilliseconds,
                    new("tool", "get_order"), new("status", "success"));
                OmsMeter.ToolCallCount.Add(1,
                    new("tool", "get_order"), new("status", "success"));
                return result.Value!;
            }
            catch (Exception ex)
            {
                sw.Stop();
                OmsMeter.ToolCallCount.Add(1, new ("tool", "get_order"), new ("status", "error"));
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        [McpServerTool(Name = "get_orders")]
        [Description(
           "Retrieve a paginated list of orders with optional status filtering. " +
           "Returns order summaries including order number, customer, total, status, and item count. " +
           "Use this to browse orders with pagination support. " +
           "For a single order's full details, use get_order instead.")]
        public async Task<PagedResult<OrderSummaryDto>?> GetOrders(
           [Description("Page number to retrieve (1-based). Default is 1.")]
            int page = 1,
           [Description("Number of items per page. Default is 10, maximum is 100.")]
            int pageSize = 10,
           [Description("Optional order status filter. Valid values: Draft, Placed, Confirmed, Shipped, Delivered, Cancelled. Leave empty to get all statuses.")]
            string? status = null)
        {
            // Validate pagination parameters
            if (page < 1)
                page = 1;
            if (pageSize < 1)
                pageSize = 10;
            if (pageSize > 100)
                pageSize = 100;

            // Parse status if provided
            OrderStatus? orderStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    orderStatus = parsedStatus;
                }
            }

            var query = new GetOrdersPagedQuery(page, pageSize, orderStatus);
            var result = await _mediator.Send(query);
            if (result.IsFailure)
            {
                return null;
            }
            return result.Value!;
        }

        [McpServerTool(Name = "place_order")]
        [Description(
            "Create a new order in the system. " +
            "Requires a valid customer ID and at least one order item. " +
            "Returns the created order ID on success. " +
            "Validates product availability and customer existence before creating.")]
        public async Task<string> PlaceOrder(
            [Description("Order details including customer, items, and optional shipping info.")]
            PlaceOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            // --- VALIDATION BLOCK ---
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                return "Error: CustomerId is required.";


            if (!Guid.TryParse(request.CustomerId, out var customerId))
                return "Error: CustomerId must be a valid GUID format " +
                       "(e.g. 3fa85f64-5717-4562-b3fc-2c963f66afa6).";


            if (request.Items == null || request.Items.Count == 0)
                return "Error: Order must contain at least one item.";


            foreach (var item in request.Items)
            {
                if (!Guid.TryParse(item.ProductId, out _))
                    return $"Error: ProductId '{item.ProductId}' is not a valid GUID.";


                if (item.Quantity < 1 || item.Quantity > 999)
                    return $"Error: Quantity for product {item.ProductId} must be between 1-999. " +
                           $"Provided: {item.Quantity}";
            }
            // --- END VALIDATION ---


            var command = new PlaceOrderCommand
            {
                CustomerId = Guid.Parse(request.CustomerId),
                ShippingAddress = new AddressDto
                {
                    Street = request.ShippingAddress.Street,
                    City = request.ShippingAddress.City,
                    Province = request.ShippingAddress.Province,
                    PostalCode = request.ShippingAddress.PostalCode,
                    Country = request.ShippingAddress.Country,
                    FormattedAddress = request.ShippingAddress.FormattedAddress
                },
                Items = request.Items.Select(i => new OrderItemDto
                {
                    ProductId = Guid.Parse(i.ProductId),
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Currency= i.Currency
                }).ToList()
            };

            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error == OrderErrors.CustomerNotFound(Guid.Parse(request.CustomerId)))
                    return $"Error: Customer with ID '{request.CustomerId}' was not found.";
                if (result.Error == OrderErrors.EmptyItems)
                    return "Error: Order must contain at least one item.";
                return "Failed to create order: " + result.Error.Description;
            }
            return $"Order created successfully. Order ID: {result.Value}";

            // Không catch generic Exception ở đây — để middleware xử lý

        }

    }
}
