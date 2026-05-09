using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Events
{
    public sealed record OrderCancelledEvent : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public Guid OrderId { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerEmail { get; init; } = string.Empty;
        public string CancellationReason { get; init; } = string.Empty;
        public DateTime CancelledAt { get; init; }
    }
}
