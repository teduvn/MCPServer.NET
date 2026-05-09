using System;
using System.Collections.Generic;

namespace OrderManagement.Application.Orders.DTOs
{
    /// <summary>
    /// DTO cho Order - sử dụng trong Application layer.
    /// Không chứa business logic, chỉ dùng để transfer data.
    /// </summary>
    public sealed record OrderDto
    {
        public Guid Id { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerEmail { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public AddressDto ShippingAddress { get; init; } = null!;
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public IReadOnlyList<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
    }
}
