using OrderManagement.Application.Orders.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Orders.DTOs
{
    public class PlaceOrderRequest
    {
        public Guid CustomerId { get; init; }
        public List<OrderItemRequest> Items { get; init; } = new List<OrderItemRequest>();

        public AddressRequest ShippingAddress { get; init; } = null!;
    }

    public class AddressRequest
    {
        public string Street { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Province { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string? PostalCode { get; init; }
    }

    public sealed record OrderItemRequest
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
