using OrderManagement.Application.Orders.Mappings;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using System;
using System.Linq;
using Xunit;

namespace OrderManagement.Tests.Unit.Application.Mappings
{
    public class OrderMappingExtensionsTests
    {
        [Fact]
        public void ToDto_Should_Map_Order_Correctly()
        {
            // Arrange
            var address = Address.Create("123 Main St", "Hanoi", "Hanoi", "VN", "100000");
            var customerId = Guid.NewGuid();
            var orderResult = Order.CreateDraft(customerId, address);
            var order = orderResult.Value;

            var product = Product.Create("Test Product", "Description", Money.FromVND(100_000), 2.0m, 100);
            order.AddItem(product.Id, product.Name, product.Price, 2);

            // Act
            var dto = order.ToDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(order.Id, dto.Id);
            Assert.Equal(order.CustomerId, dto.CustomerId);
            Assert.Equal(order.CustomerEmail, dto.CustomerEmail);
            Assert.Equal(order.Status.ToString(), dto.Status);
            Assert.Equal(order.TotalAmount.Amount, dto.TotalAmount);
            Assert.Equal(order.Currency, dto.Currency);
            Assert.Equal(order.CreatedAt, dto.CreatedAt);
            Assert.Equal(order.UpdatedAt, dto.UpdatedAt);
            Assert.NotNull(dto.ShippingAddress);
            Assert.Single(dto.Items);
        }

        [Fact]
        public void ToDto_Should_Map_OrderItem_Correctly()
        {
            // Arrange
            var address = Address.Create("123 Main St", "Hanoi", "Hanoi", "VN");
            var orderResult = Order.CreateDraft(Guid.NewGuid(), address);
            var order = orderResult.Value;
            var product = Product.Create("Test Product", "Description", Money.FromVND(50_000), 1.5m, 100);
            order.AddItem(product.Id, product.Name, product.Price, 3);

            var item = order.Items.First();

            // Act
            var dto = item.ToDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(item.Id, dto.Id);
            Assert.Equal(item.ProductId, dto.ProductId);
            Assert.Equal(item.ProductName, dto.ProductName);
            Assert.Equal(item.UnitPrice.Amount, dto.UnitPrice);
            Assert.Equal(item.UnitPrice.Currency, dto.Currency);
            Assert.Equal(item.Quantity, dto.Quantity);
            Assert.Equal(item.Subtotal.Amount, dto.Subtotal);
            Assert.Equal(150_000m, dto.Subtotal); // 50k * 3
        }

        [Fact]
        public void ToDto_Should_Map_Address_Correctly()
        {
            // Arrange
            var address = Address.Create("456 Test Ave", "Ho Chi Minh", "HCM", "VN", "700000");

            // Act
            var dto = address.ToDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(address.Street, dto.Street);
            Assert.Equal(address.City, dto.City);
            Assert.Equal(address.Province, dto.Province);
            Assert.Equal(address.Country, dto.Country);
            Assert.Equal(address.PostalCode, dto.PostalCode);
            Assert.Equal(address.ToFormattedString(), dto.FormattedAddress);
        }

        [Fact]
        public void ToDto_Should_Map_Address_Without_PostalCode()
        {
            // Arrange
            var address = Address.Create("789 Example Rd", "Da Nang", "DN", "VN");

            // Act
            var dto = address.ToDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Null(dto.PostalCode);
            Assert.Contains("Da Nang", dto.FormattedAddress);
        }

        [Fact]
        public void ToDto_Should_Map_Order_With_Multiple_Items()
        {
            // Arrange
            var address = Address.Create("123 Main St", "Hanoi", "Hanoi", "VN");
            var orderResult = Order.CreateDraft(Guid.NewGuid(), address);
            var order = orderResult.Value;

            var product1 = Product.Create("Product 1", "Desc 1", Money.FromVND(100_000), 2.0m, 100);
            var product2 = Product.Create("Product 2", "Desc 2", Money.FromVND(150_000), 3.0m, 100);
            var product3 = Product.Create("Product 3", "Desc 3", Money.FromVND(50_000), 1.0m, 100);

            order.AddItem(product1.Id, product1.Name, product1.Price, 2);
            order.AddItem(product2.Id, product2.Name, product2.Price, 1);
            order.AddItem(product3.Id, product3.Name, product3.Price, 3);

            // Act
            var dto = order.ToDto();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(3, dto.Items.Count);
            Assert.All(dto.Items, item => Assert.NotNull(item));

            var expectedTotal = (100_000m * 2) + (150_000m * 1) + (50_000m * 3); // 500,000
            Assert.Equal(expectedTotal, dto.TotalAmount);
        }

        [Fact]
        public void ToDto_Should_Preserve_OrderStatus()
        {
            // Arrange
            var address = Address.Create("123 Main St", "Hanoi", "Hanoi", "VN");
            var orderResult = Order.CreateDraft(Guid.NewGuid(), address);
            var order = orderResult.Value;

            // Act
            var dtoBeforePlaced = order.ToDto();

            var product = Product.Create("Test", "Desc", Money.FromVND(100_000), 1.0m, 100);
            order.AddItem(product.Id, product.Name, product.Price, 1);
            order.Place();

            var dtoAfterPlaced = order.ToDto();

            // Assert
            Assert.Equal("Draft", dtoBeforePlaced.Status);
            Assert.Equal("Placed", dtoAfterPlaced.Status);
        }
    }
}
