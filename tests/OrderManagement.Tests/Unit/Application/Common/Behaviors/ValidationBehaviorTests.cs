using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using OrderManagement.Application.Common.Behaviors;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Tests.Unit.Application.Common.Behaviors
{
    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_WhenValidationFails_ShouldThrowAndNotCallNext()
        {
            // Arrange
            var validator = Substitute.For<IValidator<PlaceOrderCommand>>();
            validator.ValidateAsync(Arg.Any<ValidationContext<PlaceOrderCommand>>(), Arg.Any<CancellationToken>())
                     .Returns(new ValidationResult(new[]
                     {
                 new ValidationFailure("CustomerId", "CustomerId is required")
                     }));

            var behavior = new ValidationBehavior<PlaceOrderCommand, Result<Guid>>(
                new[] { validator });

            var nextCalled = false;
            RequestHandlerDelegate<Result<Guid>> next = (cancellationToken) =>
            {
                nextCalled = true;
                return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => behavior.Handle(
                    new PlaceOrderCommand { CustomerId = Guid.Empty, ShippingAddress = new AddressDto(), Items = new List<OrderItemDto>() },
                    next,
                    CancellationToken.None));

            // Quan trọng: next() không được gọi khi validation fail
            Assert.False(nextCalled, "Handler should not be called when validation fails");
        }


    }
}
