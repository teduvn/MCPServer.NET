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
            Title = "Metadata của hệ thống OMS: version, cấu hình, trạng thái orders theo domain model. " +
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
                    new { code = "Draft",      label = "Bản nháp",
                          description = "Order mới tạo hoặc đang chỉnh sửa, chưa được đặt chính thức" },
                    new { code = "Placed",     label = "Đã đặt",
                          description = "Order đã được đặt từ trạng thái Draft" },
                    new { code = "Confirmed",  label = "Đã xác nhận",
                          description = "Order đã được xác nhận và có thể chuyển sang giao vận" },
                    new { code = "Shipped",    label = "Đã giao vận",
                          description = "Hàng đã bàn giao cho đơn vị vận chuyển" },
                    new { code = "Delivered",  label = "Đã giao thành công",
                          description = "Order đã được giao thành công" },
                    new { code = "Cancelled",  label = "Đã hủy",
                          description = "Order đã bị hủy, không thể khôi phục" }
                },
                businessRules = new
                {
                    canCancelStatuses = new[] { "Draft", "Placed", "Confirmed" },
                    nonModifiableStatuses = new[] { "Shipped", "Cancelled" },
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
