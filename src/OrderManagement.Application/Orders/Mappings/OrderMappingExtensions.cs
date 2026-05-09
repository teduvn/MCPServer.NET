using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using System.Linq;

namespace OrderManagement.Application.Orders.Mappings
{
    /// <summary>
    /// Extension methods để map từ Domain Entities sang DTOs.
    /// Đặt ở Application layer để giữ Domain layer pure.
    /// </summary>
    public static class OrderMappingExtensions
    {
        /// <summary>
        /// Map Order entity sang OrderDto.
        /// </summary>
        public static OrderDto ToDto(this Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerEmail = order.CustomerEmail,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount.Amount,
                Currency = order.Currency,
                ShippingAddress = order.ShippingAddress.ToDto(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.Items.Select(i => i.ToDto()).ToList().AsReadOnly()
            };
        }

        /// <summary>
        /// Map OrderItem entity sang OrderItemDto.
        /// </summary>
        public static OrderItemDto ToDto(this OrderItem item)
        {
            return new OrderItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice.Amount,
                Currency = item.UnitPrice.Currency,
                Quantity = item.Quantity,
                Subtotal = item.Subtotal.Amount
            };
        }

        /// <summary>
        /// Map Address Value Object sang AddressDto.
        /// </summary>
        public static AddressDto ToDto(this Address address)
        {
            return new AddressDto
            {
                Street = address.Street,
                City = address.City,
                Province = address.Province,
                Country = address.Country,
                PostalCode = address.PostalCode,
                FormattedAddress = address.ToFormattedString()
            };
        }
    }
}
