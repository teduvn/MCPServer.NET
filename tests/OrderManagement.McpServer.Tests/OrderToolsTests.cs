using FluentAssertions;
using MediatR;
using Newtonsoft.Json;
using NSubstitute;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Queries;

namespace OrderManagement.McpServer.Tests
{
    public class OrderToolsTests
    {
        private readonly IMediator _mediatorMock;
        private readonly OrderTools.OrderTools _sut;  // System Under Test


        public OrderToolsTests()
        {
            _mediatorMock = Substitute.For<IMediator>();


            // Constructor inject mock — không cần container thật
            _sut = new OrderTools.OrderTools(_mediatorMock);
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
                .Returns(expectedOrder);


            // Act: gọi tool method trực tiếp
            var result = await _sut.GetOrder(orderId);


            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(orderId);
            result.Status.Should().Be("Draft");
        }
    }

}
