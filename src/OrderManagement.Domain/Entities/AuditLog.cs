using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Entities
{
    public class AuditLog : Entity
    {

        // Ai thực hiện hành động?
        public string UserId { get; set; }       // JWT sub claim
        public string UserName { get; set; }     // Tên hiển thị
        public string ActorType { get; set; }    // 'human' | 'ai-agent' | 'system'


        // Hành động gì?
        public string CommandType { get; set; }  // Tên class Command, vd: 'CancelOrderCommand'
        public string ToolName { get; set; }     // Tên MCP tool, vd: 'cancel_order'


        // Đối tượng bị tác động?
        public string EntityType { get; set; }   // 'Order', 'Customer', ...
        public string EntityId { get; set; }     // ID của entity


        // Dữ liệu đầu vào?
        public string Parameters { get; set; }   // JSON đã sanitize


        // Kết quả?
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int DurationMs { get; set; }      // Tool chạy mất bao lâu


        // Context tracing?
        public string CorrelationId { get; set; } // Trace 1 request xuyên suốt
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
