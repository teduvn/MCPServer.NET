using System;

namespace OrderManagement.Application.Orders.DTOs
{
    /// <summary>
    /// DTO cho OrderItem.
    /// </summary>
    public sealed record OrderItemDto
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        public string Currency { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal Subtotal { get; init; }
    }
}
