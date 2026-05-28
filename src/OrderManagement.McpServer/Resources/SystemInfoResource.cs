using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OrderManagement.McpServer.Resources
{
    public class SystemInfoResource
    {
        [McpServerResource(
            UriTemplate = "oms://system/info",
            Name = "OMS System Information",
            Title = "Metadata của hệ thống OMS: version, cấu hình, trạng thái orders có thể có. " +
                          "Đọc resource này trước khi thực hiện bất kỳ action nào liên quan đến orders.",
            MimeType = "application/json"
        )]
        public Task<string> GetSystemInfo()
        {
            var info = new
            {
                system = new
                {
                    name = "OMS - Order Management System",
                    version = "1.0.0",
                    description = "Hệ thống quản lý đơn hàng xây dựng trên Clean Architecture"
                },
                orderStatuses = new[]
                {
                    new { code = "Pending",    label = "Chờ xác nhận",
                          description = "Order vừa được tạo, chưa được xử lý" },
                    new { code = "Processing", label = "Đang xử lý",
                          description = "Order đã được xác nhận, đang chuẩn bị hàng" },
                    new { code = "Shipped",    label = "Đã giao vận",
                          description = "Hàng đã bàn giao cho đơn vị vận chuyển" },
                    new { code = "Completed",  label = "Hoàn thành",
                          description = "Order đã được giao thành công" },
                    new { code = "Cancelled",  label = "Đã hủy",
                          description = "Order đã bị hủy, không thể khôi phục" }
                },
                businessRules = new
                {
                    canCancelStatuses = new[] { "Pending", "Processing" },
                    maxItemsPerOrder = 50,
                    currency = "VND"
                }
            };


            return Task.FromResult(JsonSerializer.Serialize(info, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }

}
