using FluentValidation.TestHelper;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Tests.Unit.Application.Orders.Validators
{
    public class PlaceOrderCommandValidatorTests
    {
        private readonly PlaceOrderCommandValidator _validator = new();

        [Fact]
        public async Task Should_HaveError_When_CustomerIdIsEmpty()
        {
            // Arrange
            var command = new PlaceOrderCommand
            {
                CustomerId = Guid.Empty,      // <-- lỗi ở đây
                Items = ValidItems(),
                ShippingAddress = ValidAddress()
            };

            // Act
            var result = await _validator.TestValidateAsync(command);

            // Assert — FluentValidation.TestHelper helper
            result.ShouldHaveValidationErrorFor(x => x.CustomerId)
                  .WithErrorMessage("CustomerId không được để trống");
        }

        [Fact]
        public async Task Should_HaveError_When_ItemsIsEmpty()
        {
            var command = new PlaceOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<OrderItemDto>(),             // <-- danh sách rỗng
                ShippingAddress = ValidAddress()
            };

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.Items);
        }

        [Fact]
        public async Task Should_NotHaveError_When_CommandIsValid()
        {
            var command = ValidCommand();
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        // --- Helpers ---
        private static PlaceOrderCommand ValidCommand() => new PlaceOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = ValidItems(),
            ShippingAddress = ValidAddress(),
        };
        private static List<OrderItemDto> ValidItems() =>
            new List<OrderItemDto> { new OrderItemDto() {
            ProductName = "prod-001", Quantity = 2, UnitPrice = 150_000m, ProductId = Guid.NewGuid()
            }  };

        private static AddressDto ValidAddress() =>
            new()
            {
                Street = "123 Main St",
                City = "Hanoi",
                Province = "HN",
                Country = "VN",
                PostalCode = "100000"
            };
    }

}
