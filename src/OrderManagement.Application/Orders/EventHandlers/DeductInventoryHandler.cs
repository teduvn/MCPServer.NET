using MediatR;
using OrderManagement.Application.Common.Events;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Orders.EventHandlers
{
    internal sealed class DeductInventoryHandler
     : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
    {
        private readonly IInventoryService _inventoryService;

        public DeductInventoryHandler(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public async Task Handle(
            DomainEventNotification<OrderPlacedEvent> notification,
            CancellationToken cancellationToken)
        {
            var @event = notification.DomainEvent;

            foreach (var item in @event.Items)
            {
                await _inventoryService.DeductAsync(
                    productId: item.ProductId,
                    quantity: item.Quantity,
                    cancellationToken: cancellationToken);
            }
        }
    }

}
