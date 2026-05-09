using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderManagement.Application.Common.Behaviors;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Tests.Unit.Application.Common.Behaviors
{
    // Test command for unit testing
    public record TestCommand : IRequest<Result<Guid>>
    {
        public string Name { get; init; } = "Test";
    }

    public class LoggingBehaviorTests
    {
        [Fact]
        public async Task Handle_ShouldLog_BeforeAndAfterHandler()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestCommand, Result<Guid>>>>();
            var correlationIdService = Substitute.For<ICorrelationIdService>();
            correlationIdService.CorrelationId.Returns("test-correlation-id");
            var behavior = new LoggingBehavior<TestCommand, Result<Guid>>(logger, correlationIdService);
            var command = new TestCommand();
            var expectedGuid = Guid.NewGuid();
            var expectedResult = Result<Guid>.Success(expectedGuid);

            // next() giả lập handler trả về kết quả
            RequestHandlerDelegate<Result<Guid>> next =
                (cancellationToken) => Task.FromResult(expectedResult);

            // Act
            var result = await behavior.Handle(command, next, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedGuid, result.Value);

            // Kiểm tra logger được gọi 2 lần với LogInformation (trước và sau)
            logger.Received(2).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task Handle_WhenExceptionThrown_ShouldLogError()
        {
            // Arrange
            var logger = Substitute.For<ILogger<LoggingBehavior<TestCommand, Result<Guid>>>>();
            var correlationIdService = Substitute.For<ICorrelationIdService>();
            correlationIdService.CorrelationId.Returns("test-correlation-id");
            var behavior = new LoggingBehavior<TestCommand, Result<Guid>>(logger, correlationIdService);
            var command = new TestCommand();
            var expectedException = new InvalidOperationException("Test exception");

            // next() giả lập handler ném exception
            RequestHandlerDelegate<Result<Guid>> next =
                (cancellationToken) => throw expectedException;

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.Handle(command, next, CancellationToken.None));

            Assert.Equal(expectedException, thrownException);

            // Kiểm tra log Information được gọi 1 lần (trước handler)
            logger.Received(1).Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());

            // Kiểm tra log Error được gọi 1 lần (sau khi exception)
            logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Is<Exception>(ex => ex == expectedException),
                Arg.Any<Func<object, Exception?, string>>());
        }
    }


}
