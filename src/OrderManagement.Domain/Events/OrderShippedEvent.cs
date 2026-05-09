using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Events
{
    public sealed record OrderShippedEvent : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid OrderId { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerEmail { get; init; } = string.Empty;
        public string TrackingNumber { get; init; } = string.Empty;
        public DateTime EstimatedDelivery { get; init; }
    }

}
