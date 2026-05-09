using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Events
{
    public sealed record OrderPlacedEvent : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        // Data cần thiết cho handler
        public Guid OrderId { get; init; }
        public Guid CustomerId { get; init; }

        public string CustomerName { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public string CustomerEmail { get; init; } = string.Empty;
        public IReadOnlyList<OrderItemSnapshot> Items { get; init; } = [];
    }

    // Snapshot vì OrderItem có thể thay đổi sau này
    public sealed record OrderItemSnapshot(
        Guid ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice
    );
}
