using FluentAssertions;
using MediatR;
using NSubstitute;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Queries;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Orders;
using OrderManagement.McpServer.Models;
using OrderManagement.McpServer.Tools;

namespace OrderManagement.McpServer.Tests.Tools
{
    public class OrderToolsTests
    {
        private readonly IMediator _mediatorMock;
        private readonly OrderTools _sut;  // System Under Test


        public OrderToolsTests()
        {
            _mediatorMock = Substitute.For<IMediator>();


            // Constructor inject mock — không cần container thật
            _sut = new OrderTools(_mediatorMock);
        }


        [Fact]
        public async Task GetOrder_ValidId_ReturnsOrderJson()
        {
            // Arrange: mock MediatR trả về order giả
            var orderId = Guid.NewGuid();
            var expectedOrder = new OrderDto
            {
                Id = orderId,
                Status = "Draft",
                TotalAmount = 150_000
            };


            _mediatorMock
                .Send(Arg.Any<GetOrderByIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result<OrderDto?>.Success(expectedOrder));


            // Act: gọi tool method trực tiếp
            var result = await _sut.GetOrder(orderId);


            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(orderId);
            result.Status.Should().Be("Draft");
        }

        [Fact]
        public async Task GetOrderAsync_WhenOrderExists_ReturnsSerializedOrder()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var expectedOrder = new OrderDto
            {
                Id = orderId,
                CustomerEmail = "Nguyen Van A",
                Status = "Draft",
                TotalAmount = 500_000m
            };
            _mediatorMock
                .Send(Arg.Is<GetOrderByIdQuery>(q => q.OrderId == orderId), Arg.Any<CancellationToken>())
                .Returns(Result<OrderDto?>.Success(expectedOrder));


            // Act
            var result = await _sut.GetOrder(orderId);


            // Assert
            result.Should().NotBeNull();
        }


        [Fact]
        public async Task GetOrderAsync_WhenOrderNotFound_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _mediatorMock
                .Send(Arg.Any<GetOrderByIdQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result<OrderDto?>.Failure(new Error("Order.NotFound", "Order not found")));


            // Act
            var result = await _sut.GetOrder(nonExistentId);


            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetOrders_DefaultParameters_ReturnsPagedResult()
        {
            // Arrange
            var orders = new List<OrderSummaryDto>
            {
                new OrderSummaryDto
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "ORD-001",
                    CustomerName = "Nguyen Van A",
                    Status = "Draft",
                    TotalAmount = 100_000,
                    Currency = "VND",
                    ItemCount = 2
                },
                new OrderSummaryDto
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "ORD-002",
                    CustomerName = "Tran Thi B",
                    Status = "Placed",
                    TotalAmount = 200_000,
                    Currency = "VND",
                    ItemCount = 3
                }
            };

            var pagedResult = new PagedResult<OrderSummaryDto>(orders, 2, 1, 10);

            _mediatorMock
                .Send(Arg.Any<GetOrdersPagedQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result<PagedResult<OrderSummaryDto>>.Success(pagedResult));

            // Act
            var result = await _sut.GetOrders();

            // Assert
            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(2);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetOrders_WithStatusFilter_ReturnsFilteredOrders()
        {
            // Arrange
            var orders = new List<OrderSummaryDto>
            {
                new OrderSummaryDto
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "ORD-001",
                    CustomerName = "Nguyen Van A",
                    Status = "Placed",
                    TotalAmount = 100_000,
                    Currency = "VND",
                    ItemCount = 2
                }
            };

            var pagedResult = new PagedResult<OrderSummaryDto>(orders, 1, 1, 10);

            _mediatorMock
                .Send(Arg.Is<GetOrdersPagedQuery>(q => 
                    q.Status == OrderStatus.Placed), 
                    Arg.Any<CancellationToken>())
                .Returns(Result<PagedResult<OrderSummaryDto>>.Success(pagedResult));

            // Act
            var result = await _sut.GetOrders(status: "Placed");

            // Assert
            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(1);
            result.Items.First().Status.Should().Be("Placed");
        }

        [Fact]
        public async Task GetOrders_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var orders = new List<OrderSummaryDto>
            {
                new OrderSummaryDto
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "ORD-011",
                    CustomerName = "Customer 11",
                    Status = "Draft",
                    TotalAmount = 100_000,
                    Currency = "VND",
                    ItemCount = 1
                }
            };

            var pagedResult = new PagedResult<OrderSummaryDto>(orders, 25, 2, 10);

            _mediatorMock
                .Send(Arg.Is<GetOrdersPagedQuery>(q => 
                    q.Page == 2 && q.PageSize == 10), 
                    Arg.Any<CancellationToken>())
                .Returns(Result<PagedResult<OrderSummaryDto>>.Success(pagedResult));

            // Act
            var result = await _sut.GetOrders(page: 2, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result!.Page.Should().Be(2);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetOrders_WhenFails_ReturnsNull()
        {
            // Arrange
            _mediatorMock
                .Send(Arg.Any<GetOrdersPagedQuery>(), Arg.Any<CancellationToken>())
                .Returns(Result<PagedResult<OrderSummaryDto>>.Failure(new Error("Query.Failed", "Failed to retrieve orders")));

            // Act
            var result = await _sut.GetOrders();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PlaceOrder_ValidRequest_ReturnsSuccessMessage()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var orderId = Guid.NewGuid();

            var request = new PlaceOrderRequest
            {
                CustomerId = customerId.ToString(),
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest
                    {
                        ProductId = productId.ToString(),
                        Quantity = 2,
                        UnitPrice = 50_000,
                        Currency = "VND"
                    }
                },
                ShippingAddress = new ShippingAddressRequest
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi",
                    Country = "Vietnam",
                    PostalCode = "100000",
                    FormattedAddress = "123 Main St, Hanoi, Vietnam"
                }
            };

            _mediatorMock
                .Send(Arg.Any<PlaceOrderCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result<Guid>.Success(orderId));

            // Act
            var result = await _sut.PlaceOrder(request);

            // Assert
            result.Should().Contain("Order created successfully");
            result.Should().Contain(orderId.ToString());
        }

        [Fact]
        public async Task PlaceOrder_EmptyCustomerId_ReturnsValidationError()
        {
            // Arrange
            var request = new PlaceOrderRequest
            {
                CustomerId = "",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest
                    {
                        ProductId = Guid.NewGuid().ToString(),
                        Quantity = 1,
                        UnitPrice = 50_000,
                        Currency = "VND"
                    }
                },
                ShippingAddress = new ShippingAddressRequest
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi"
                }
            };

            // Act
            var result = await _sut.PlaceOrder(request);

            // Assert
            result.Should().Contain("Error: CustomerId is required");
        }

        [Fact]
        public async Task PlaceOrder_InvalidCustomerIdFormat_ReturnsValidationError()
        {
            // Arrange
            var request = new PlaceOrderRequest
            {
                CustomerId = "invalid-guid",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest
                    {
                        ProductId = Guid.NewGuid().ToString(),
                        Quantity = 1,
                        UnitPrice = 50_000,
                        Currency = "VND"
                    }
                },
                ShippingAddress = new ShippingAddressRequest
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi"
                }
            };

            // Act
            var result = await _sut.PlaceOrder(request);

            // Assert
            result.Should().Contain("Error: CustomerId must be a valid GUID format");
        }

        [Fact]
        public async Task PlaceOrder_EmptyItems_ReturnsValidationError()
        {
            // Arrange
            var request = new PlaceOrderRequest
            {
                CustomerId = Guid.NewGuid().ToString(),
                Items = new List<OrderItemRequest>(),
                ShippingAddress = new ShippingAddressRequest
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi"
                }
            };

            // Act
            var result = await _sut.PlaceOrder(request);

            // Assert
            result.Should().Contain("Error: Order must contain at least one item");
        }

        [Fact]
        public async Task PlaceOrder_InvalidProductIdFormat_ReturnsValidationError()
        {
            // Arrange
            var request = new PlaceOrderRequest
            {
                CustomerId = Guid.NewGuid().ToString(),
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest
                    {
                        ProductId = "invalid-product-id",
                        Quantity = 1,
                        UnitPrice = 50_000,
                        Currency = "VND"
                    }
                },
                ShippingAddress = new ShippingAddressRequest
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi"
                }
            };

            // Act
            var result = await _sut.PlaceOrder(request);

            // Assert
            result.Should().Contain("Error: ProductId");
            result.Should().Contain("is not a valid GUID");
        }

        [Fact]
        public async Task PlaceOrder_InvalidQuantity_ReturnsValidationError()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var request = new PlaceOrderRequest
            {
                CustomerId = Guid.NewGuid().ToString(),
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest
                    {
                        ProductId = productId.ToString(),
                        Quantity = 0,
                        UnitPrice = 50_000,
                        Currency = "VND"
                    }
                },
                ShippingAddress = new ShippingAddressRequest
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi"
                }
            };

            // Act
            var result = await _sut.PlaceOrder(request);

            // Assert
            result.Should().Contain("Error: Quantity");
            result.Should().Contain("must be between 1-999");
        }

        [Fact]
        public async Task PlaceOrder_CustomerNotFound_ReturnsError()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var request = new PlaceOrderRequest
            {
                CustomerId = customerId.ToString(),
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest
                    {
                        ProductId = Guid.NewGuid().ToString(),
                        Quantity = 1,
                        UnitPrice = 50_000,
                        Currency = "VND"
                    }
                },
                ShippingAddress = new ShippingAddressRequest
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi"
                }
            };

            _mediatorMock
                .Send(Arg.Any<PlaceOrderCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result<Guid>.Failure(OrderErrors.CustomerNotFound(customerId)));

            // Act
            var result = await _sut.PlaceOrder(request);

            // Assert
            result.Should().Contain($"Error: Customer with ID '{customerId}' was not found");
        }

        [Fact]
        public async Task PlaceOrder_MultipleItems_ReturnsSuccess()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new PlaceOrderRequest
            {
                CustomerId = Guid.NewGuid().ToString(),
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest
                    {
                        ProductId = Guid.NewGuid().ToString(),
                        Quantity = 2,
                        UnitPrice = 50_000,
                        Currency = "VND"
                    },
                    new OrderItemRequest
                    {
                        ProductId = Guid.NewGuid().ToString(),
                        Quantity = 1,
                        UnitPrice = 100_000,
                        Currency = "VND"
                    }
                },
                ShippingAddress = new ShippingAddressRequest
                {
                    Street = "456 Second St",
                    City = "Ho Chi Minh",
                    Province = "Ho Chi Minh"
                }
            };

            _mediatorMock
                .Send(Arg.Any<PlaceOrderCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result<Guid>.Success(orderId));

            // Act
            var result = await _sut.PlaceOrder(request);

            // Assert
            result.Should().Contain("Order created successfully");
            result.Should().Contain(orderId.ToString());
        }

        [Fact]
        public async Task GetOrders_InvalidPageNumber_UsesDefaultValue()
        {
            // Arrange
            var orders = new List<OrderSummaryDto>
            {
                new OrderSummaryDto
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "ORD-001",
                    CustomerName = "Test Customer",
                    Status = "Draft",
                    TotalAmount = 100_000,
                    Currency = "VND",
                    ItemCount = 1
                }
            };

            var pagedResult = new PagedResult<OrderSummaryDto>(orders, 1, 1, 10);

            _mediatorMock
                .Send(Arg.Is<GetOrdersPagedQuery>(q => q.Page == 1), Arg.Any<CancellationToken>())
                .Returns(Result<PagedResult<OrderSummaryDto>>.Success(pagedResult));

            // Act
            var result = await _sut.GetOrders(page: -1);

            // Assert
            result.Should().NotBeNull();
            result!.Page.Should().Be(1);
        }

        [Fact]
        public async Task GetOrders_InvalidPageSize_UsesDefaultValue()
        {
            // Arrange
            var orders = new List<OrderSummaryDto>();
            var pagedResult = new PagedResult<OrderSummaryDto>(orders, 0, 1, 10);

            _mediatorMock
                .Send(Arg.Is<GetOrdersPagedQuery>(q => q.PageSize == 10), Arg.Any<CancellationToken>())
                .Returns(Result<PagedResult<OrderSummaryDto>>.Success(pagedResult));

            // Act
            var result = await _sut.GetOrders(pageSize: 0);

            // Assert
            result.Should().NotBeNull();
            result!.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetOrders_PageSizeExceedsMaximum_UsesMaxValue()
        {
            // Arrange
            var orders = new List<OrderSummaryDto>();
            var pagedResult = new PagedResult<OrderSummaryDto>(orders, 0, 1, 100);

            _mediatorMock
                .Send(Arg.Is<GetOrdersPagedQuery>(q => q.PageSize == 100), Arg.Any<CancellationToken>())
                .Returns(Result<PagedResult<OrderSummaryDto>>.Success(pagedResult));

            // Act
            var result = await _sut.GetOrders(pageSize: 200);

            // Assert
            result.Should().NotBeNull();
            result!.PageSize.Should().Be(100);
        }

        [Fact]
        public async Task GetOrders_InvalidStatus_IgnoresFilter()
        {
            // Arrange
            var orders = new List<OrderSummaryDto>();
            var pagedResult = new PagedResult<OrderSummaryDto>(orders, 0, 1, 10);

            _mediatorMock
                .Send(Arg.Is<GetOrdersPagedQuery>(q => q.Status == null), Arg.Any<CancellationToken>())
                .Returns(Result<PagedResult<OrderSummaryDto>>.Success(pagedResult));

            // Act
            var result = await _sut.GetOrders(status: "InvalidStatus");

            // Assert
            result.Should().NotBeNull();
        }
    }
}
