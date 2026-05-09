using FluentAssertions;
using NSubstitute;
using OrderManagement.Application.Common.Exceptions;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Tests.Unit.Application.Orders
{
    public class PlaceOrderCommandHandlerTests
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PlaceOrderCommandHandler _sut;
        private readonly IEmailService _emailService = Substitute.For<IEmailService>();
        private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
        private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

        public PlaceOrderCommandHandlerTests()
        {
            // Tạo mock — không cần DI container
            _orderRepo = Substitute.For<IOrderRepository>();
            _customerRepo = Substitute.For<ICustomerRepository>();
            _unitOfWork = Substitute.For<IUnitOfWork>();

            // Mock authenticated user
            _currentUserService.UserId.Returns(Guid.NewGuid());
            _currentUserService.IsAuthenticated.Returns(true);

            _sut = new PlaceOrderCommandHandler(
                _orderRepo, _customerRepo, _unitOfWork, _emailService, _paymentGateway, _currentUserService);
        }

        [Fact]
        public async Task Handle_UnauthenticatedUser_ShouldReturnUnauthorizedError()
        {
            // Arrange
            var unauthenticatedUserService = Substitute.For<ICurrentUserService>();
            unauthenticatedUserService.UserId.Returns((Guid?)null);
            unauthenticatedUserService.IsAuthenticated.Returns(false);

            var handler = new PlaceOrderCommandHandler(
                _orderRepo, 
                _customerRepo, 
                _unitOfWork, 
                _emailService, 
                _paymentGateway, 
                unauthenticatedUserService);

            var command = new PlaceOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                ShippingAddress = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi",
                    Country = "Vietnam",
                    PostalCode = "100000"
                },
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Product A",
                        UnitPrice = 15.00m,
                        Currency = "VND",
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("UNAUTHORIZED");
            result.Error.Description.Should().Be("User chưa đăng nhập.");

            // Verify: không có thao tác nào được thực hiện khi unauthenticated
            _orderRepo.DidNotReceive().Add(Arg.Any<Order>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }


        // Test 1: Happy path — order được tạo thành công
        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessWithOrderId()
        {
            // Arrange
            // Arrange: payment gateway trả về success
            _paymentGateway
                .ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>())
                .Returns(new PaymentResult(true, "txn_123", null, null));

            var customerId = Guid.NewGuid();
            var customerResult = Customer.Create("John", "Doe", "john.doe@example.com", "123456789");
            var customer = customerResult.Value;
            _customerRepo.GetByIdAsync(customerId, default)
                .Returns(customer);

            var command = new PlaceOrderCommand
            {
                CustomerId = customerId,
                ShippingAddress = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi",
                    Country = "Vietnam",
                    PostalCode = "100000"
                },
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Product A",
                        UnitPrice = 15.00m,
                        Currency = "VND",
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
            // Verify repo được gọi đúng 1 lần
            _orderRepo.Received(1).Add(Arg.Any<Order>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

            // Verify: email phải được gửi đúng 1 lần với đúng email
            await _emailService
                .Received(1)
                .SendOrderConfirmationAsync(
                    "john.doe@example.com",
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<decimal>(),
                    Arg.Any<CancellationToken>());

        }

        [Fact]
        public async Task Handle_PaymentFailed_ReturnsFailureAndDoesNotSendEmail()
        {
            // Arrange
            // Arrange: payment gateway trả về thất bại
            _paymentGateway
                .ChargeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>())
                .Returns(new PaymentResult(false, null, "CARD_DECLINED", "Your card was declined"));

            var customerId = Guid.NewGuid();
            var customerResult = Customer.Create("John", "Doe", "john.doe@example.com", "123456789");
            var customer = customerResult.Value;
            _customerRepo.GetByIdAsync(customerId, default)
                .Returns(customer);

            var command = new PlaceOrderCommand
            {
                CustomerId = customerId,
                ShippingAddress = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi",
                    Country = "Vietnam",
                    PostalCode = "100000"
                },
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Product A",
                        UnitPrice = 15.00m,
                        Currency = "VND",
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CARD_DECLINED");

            // Verify: email KHÔNG được gửi khi thanh toán thất bại
            await _emailService
                .DidNotReceive()
                .SendOrderConfirmationAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<decimal>(),
                    Arg.Any<CancellationToken>());

            // Verify: order KHÔNG được save vào DB
             _orderRepo
                .DidNotReceive()
                .Add(Arg.Any<Order>());
        }

        // Test 2: Business error — customer không tồn tại
        [Fact]
        public async Task Handle_CustomerNotFound_ReturnsFailure()
        {
            // Arrange — mock trả về null
            _customerRepo.GetByIdAsync(Arg.Any<Guid>(), default)
                .Returns((Customer?)null);

            var command = new PlaceOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                ShippingAddress = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi",
                    Country = "Vietnam",
                    PostalCode = "100000"
                },
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Product A",
                        UnitPrice = 20.00m,
                        Currency = "VND",
                        Quantity = 1
                    }
                }
            };

            // Act
            var result = await _sut.Handle(command, default);

            // Assert — KHÔNG có try/catch, assert trực tiếp
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().StartWith("Order.CustomerNotFound");

            // Verify repo KHÔNG được gọi
            _orderRepo.DidNotReceive().Add(Arg.Any<Order>());
        }

        // Test 3: Business error — order rỗng
        [Fact]
        public async Task Handle_EmptyItems_ReturnsOrderEmptyItemsError()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var customerResult = Customer.Create("Jane", "Smith", "jane.smith@example.com", "987654321");
            var customer = customerResult.Value;
            _customerRepo.GetByIdAsync(customerId, default)
                .Returns(customer);

            var command = new PlaceOrderCommand
            {
                CustomerId = customerId,
                ShippingAddress = new AddressDto
                {
                    Street = "456 Oak Ave",
                    City = "Hanoi",
                    Province = "Hanoi",
                    Country = "Vietnam",
                    PostalCode = "100000"
                },
                Items = new List<OrderItemDto>() // Không có item
            };

            // Act
            var result = await _sut.Handle(command, default);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(OrderErrors.EmptyItems);
        }

    }
}
