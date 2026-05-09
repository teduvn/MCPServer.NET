using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Common.Events;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Events;

namespace OrderManagement.Application.Orders.EventHandlers
{
    internal sealed class SendOrderConfirmationEmailHandler : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<SendOrderConfirmationEmailHandler> _logger;

        public SendOrderConfirmationEmailHandler(
            IEmailService emailService,
            ILogger<SendOrderConfirmationEmailHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<OrderPlacedEvent> notification,
            CancellationToken cancellationToken)
        {
            var @event = notification.DomainEvent;

            _logger.LogInformation(
                "Sending confirmation email for order {OrderId} to {Email}",
                @event.OrderId,
                @event.CustomerEmail);

            await _emailService.SendOrderConfirmationAsync(
                toEmail: @event.CustomerEmail,
                customerName: @event.CustomerName,
                orderId: @event.OrderId,
                totalAmount: @event.TotalAmount,
                cancellationToken: cancellationToken);
        }
    }

}
