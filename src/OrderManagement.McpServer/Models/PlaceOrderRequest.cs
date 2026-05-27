using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OrderManagement.McpServer.Models
{
    /// <summary>
    /// Request model cho tool place_order.
    /// Mỗi property cần [Description] — AI đọc để biết truyền gì.
    /// </summary>
    public class PlaceOrderRequest
    {
        [Description("Customer ID in GUID format. Must exist in the system.")]
        public required string CustomerId { get; set; }


        [Description(
            "List of items to order. Must contain at least one item. " +
            "Each item requires productId and quantity.")]
        public required List<OrderItemRequest> Items { get; set; }


        [Description(
            "Shipping address. If null, uses the customer's default address on file.")]
        public ShippingAddressRequest? ShippingAddress { get; set; }


        [Description(
            "Optional note from customer (max 500 characters). " +
            "Use for special delivery instructions.")]
        public string? Note { get; set; }
    }


    public class OrderItemRequest
    {
        [Description("Product ID in GUID format.")]
        public required string ProductId { get; set; }


        [Description("Number of units to order. Must be between 1 and 999.")]
        public required int Quantity { get; set; }

        [Description("Unit price of the product.")]
        public required decimal UnitPrice { get; set; } = 0;

        [Description(
            "Currency code in ISO 4217 format (e.g. 'USD', 'VND'). " +
            "Must be a valid 3-letter currency code.")]
        public required string Currency { get; set; } = "VND";
    }


    public class ShippingAddressRequest
    {
        [Description("Street address, house number, apartment number.")]
        public required string Street { get; set; }


        [Description("City name.")]
        public required string City { get; set; }


        [Description("Province or state.")]
        public required string Province { get; set; }

        [Description("Country name.")]
        public string Country { get; init; } = string.Empty;

        [Description("Postal code or ZIP code.")]
        public string? PostalCode { get; init; }

        [Description(
            "Formatted address for display purposes. " +
            "If not provided, the system will generate one from the other fields.")]
        public string FormattedAddress { get; init; } = string.Empty;
    }

}
