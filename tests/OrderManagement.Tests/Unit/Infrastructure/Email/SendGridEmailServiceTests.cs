using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OrderManagement.Infrastructure.Email;
using OrderManagement.Infrastructure.Resilience;
using Polly.CircuitBreaker;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace OrderManagement.Tests.Unit.Infrastructure.Email
{
    public class SendGridEmailServiceTests
    {
        private readonly ISendGridClient _mockClient;
        private readonly ILogger<SendGridEmailService> _mockLogger;
        private readonly SendGridEmailService _sut;

        public SendGridEmailServiceTests()
        {
            _mockClient = Substitute.For<ISendGridClient>();
            _mockLogger = Substitute.For<ILogger<SendGridEmailService>>();

            var options = Options.Create(new SendGridSettings
            {
                ApiKey = "test-key",
                SenderEmail = "test@tedu.com.vn",
                SenderName = "Test"
            });

            _sut = new SendGridEmailService(_mockClient, options, _mockLogger);
        }

        [Fact]
        public async Task SendOrderConfirmation_WhenSendGridSucceeds_ShouldNotThrow()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var customerName = "Test Customer";
            var totalAmount = 150.00m;
            var mockResponse = new Response(
                HttpStatusCode.Accepted, null, null);

            _mockClient
                .SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
                .Returns(mockResponse);

            // Act & Assert — không throw
            var act = () => _sut.SendOrderConfirmationAsync(
                "user@test.com", customerName, orderId, totalAmount);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendOrderConfirmation_WhenSendGridFails_ShouldThrowEmailDeliveryException()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var customerName = "Test Customer";
            var totalAmount = 150.00m;
            var failResponse = new Response(
                HttpStatusCode.ServiceUnavailable, null, null);

            _mockClient
                .SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
                .Returns(failResponse);

            // Act
            var act = () => _sut.SendOrderConfirmationAsync(
                "user@test.com", customerName, orderId, totalAmount);

            // Assert
            await act.Should().ThrowAsync<EmailDeliveryException>()
                .WithMessage("*Failed to send*");
        }

        // Verify Circuit Breaker mở sau N lần fail liên tiếp
        [Fact]
        public async Task CircuitBreaker_ShouldOpen_AfterConsecutiveFailures()
        {
            // Arrange — tạo HttpClient với Circuit Breaker policy
            var circuitBreakerPolicy = ResiliencePolicies.GetCircuitBreakerPolicy();

            // Simulate 5 consecutive failures
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await circuitBreakerPolicy.ExecuteAsync(
                    () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
                }
                catch { /* expected */ }
            }

            // Act — lần thứ 6 phải BrokenCircuitException (không chờ network)
            var act = () => circuitBreakerPolicy.ExecuteAsync(
                () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

            // Assert
            await act.Should().ThrowAsync<BrokenCircuitException>();
        }


    }

}
