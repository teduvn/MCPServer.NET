

using OrderManagement.Domain.Common;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Domain.Orders
{
    public static class OrderErrors
    {
        // Đặt tên theo pattern: Entity.Reason
        public static readonly Error NotFound =
            Error.Create("Order.NotFound", "Order was not found.");

        public static readonly Error EmptyItems =
            Error.Create("Order.EmptyItems", "Order must contain at least one item.");

        public static readonly Error BelowMinimum =
            Error.Create("Order.BelowMinimum", "Order total must be at least $10.");

        public static readonly Error AlreadyShipped =
            Error.Create("Order.AlreadyShipped", "Cannot cancel an order that is already shipped.");

        // Error phụ thuộc vào data — dùng factory method
        public static Error CustomerNotFound(Guid customerId) =>
            Error.Create("Order.CustomerNotFound", $"Customer {customerId} does not exist.");

        public static Error ShippingAddressNotFound(Address? address) =>
            Error.Create("Order.ShippingAddressNotFound", $"Shipping address {address} does not exist.");
    }
}
