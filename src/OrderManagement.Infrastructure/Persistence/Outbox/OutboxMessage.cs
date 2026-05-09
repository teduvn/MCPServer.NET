using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Outbox
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();


        // Tên type đầy đủ để deserialize đúng: "Domain.Events.OrderPlacedEvent"
        public string Type { get; set; } = string.Empty;


        // JSON serialized domain event
        public string Content { get; set; } = string.Empty;


        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;


        // null = chưa xử lý; có giá trị = đã xử lý thành công
        public DateTime? ProcessedAt { get; set; }


        // Lỗi nếu xử lý thất bại (để debug)
        public string? Error { get; set; }
    }

}
