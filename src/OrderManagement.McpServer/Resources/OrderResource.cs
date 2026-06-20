using MediatR;
using ModelContextProtocol.Server;
using OrderManagement.Application.Orders.Queries;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OrderManagement.McpServer.Resources
{
    public class OrderResource
    {
        private readonly IMediator _mediator;
        private static readonly string[] AvailableStatuses =
            Enum.GetNames(typeof(OrderStatus));


        // Constructor injection — SDK tự inject qua DI
        public OrderResource(IMediator mediator)
        {
            _mediator = mediator;
        }


        // --- Single order resource ---
        [McpServerResource(
            UriTemplate = "oms://orders/{orderId}",
            Name = "Order Detail",
            Title = "Chi tiết một order với các field thực tế từ DTO: id, customerId, customerEmail, status, totalAmount, currency, shippingAddress, createdAt, updatedAt, items. Status hợp lệ: Draft, Placed, Confirmed, Shipped, Delivered, Cancelled.",
            MimeType = "application/json"
        )]
        public async Task<string> GetOrder(Guid orderId)
        {
            // Tái sử dụng Query đã có trong Application Layer
            var query = new GetOrderByIdQuery(orderId);
            var order = await _mediator.Send(query);


            if (order is null)
            {
                // Trả JSON với error — không throw exception
                // AI cần đọc được error message để xử lý tiếp
                return JsonSerializer.Serialize(new
                {
                    error = "NOT_FOUND",
                    message = $"Order với ID '{orderId}' không tồn tại trong hệ thống.",
                    suggestion = "Dùng resource oms://orders/list để xem danh sách order hợp lệ"
                });
            }


            var response = new
            {
                data = order,
                meta = new
                {
                    availableStatuses = AvailableStatuses,
                    fields = new[]
                    {
                        "id",
                        "customerId",
                        "customerEmail",
                        "status",
                        "totalAmount",
                        "currency",
                        "shippingAddress",
                        "createdAt",
                        "updatedAt",
                        "items"
                    }
                }
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }


        [McpServerResource(
            UriTemplate = "oms://orders/list",
            Name = "Orders List",
            Title = "Danh sách orders với filter và pagination. Field của mỗi item: id, orderNumber, orderDate, customerName (đang chứa customerEmail), totalAmount, currency, status, itemCount. Status hợp lệ: Draft, Placed, Confirmed, Shipped, Delivered, Cancelled.",
            MimeType = "application/json"
        )]
        public async Task<string> ListOrders(
            OrderStatus? status = null,   // Filter theo status, null = lấy tất cả
            int page = 1,            // Trang hiện tại
            int pageSize = 20)       // Số records mỗi trang
        {
            // Validate pagination params
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);  // Tối đa 100, tránh AI spam

            var query = new GetOrdersPagedQuery(page, pageSize, status);

            var result = await _mediator.Send(query);

            if (result.IsFailure)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "ERROR",
                    message = result.Error.Description,
                    code = result.Error.Code
                });
            }

            var pagedResult = result.Value;

            // Trả về metadata pagination cùng với data
            // AI cần biết totalCount để tự quyết định có cần đọc thêm trang không
            var response = new
            {
                data = pagedResult.Items,
                pagination = new
                {
                    currentPage = pagedResult.Page,
                    pageSize = pagedResult.PageSize,
                    totalCount = pagedResult.TotalCount,
                    totalPages = pagedResult.TotalPages,
                    hasNextPage = pagedResult.HasNextPage,
                    hasPreviousPage = pagedResult.HasPreviousPage
                },
                filter = new { status = status?.ToString() ?? "all" },
                meta = new
                {
                    availableStatuses = AvailableStatuses,
                    fields = new[]
                    {
                        "id",
                        "orderNumber",
                        "orderDate",
                        "customerName",
                        "totalAmount",
                        "currency",
                        "status",
                        "itemCount"
                    },
                    fieldNotes = new
                    {
                        customerName = "This field currently contains CustomerEmail from the underlying DTO/query projection.",
                        orderDate = "This field is mapped from CreatedAt."
                    }
                }
            };


            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

    }

}
