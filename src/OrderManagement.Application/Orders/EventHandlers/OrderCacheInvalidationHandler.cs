using MediatR;
using OrderManagement.Application.Common.Constants;
using OrderManagement.Application.Common.Events;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Events;

namespace OrderManagement.Application.Orders.EventHandlers
{
    /// <summary>
    /// Lắng nghe domain events liên quan đến Order,
    /// xóa cache khi data thay đổi.
    /// Command Handler không cần biết cache — đây là responsibility của class này.
    /// </summary>
    public sealed class OrderCacheInvalidationHandler(ICacheService cacheService)
        : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>,
          INotificationHandler<DomainEventNotification<OrderShippedEvent>>,
          INotificationHandler<DomainEventNotification<OrderCancelledEvent>>
    {
        // Xử lý khi đặt đơn mới
        public async Task Handle(DomainEventNotification<OrderPlacedEvent> notification,
            CancellationToken ct)
        {
            var @event = notification.DomainEvent;
            await InvalidateOrderCache(
                @event.OrderId,
                @event.CustomerId,
                ct);
        }

        // Xử lý khi đơn được ship
        public async Task Handle(DomainEventNotification<OrderShippedEvent> notification,
            CancellationToken ct)
        {
            var @event = notification.DomainEvent;
            await InvalidateOrderCache(
                @event.OrderId,
                @event.CustomerId,
                ct);
        }

        // Xử lý khi đơn bị hủy
        public async Task Handle(DomainEventNotification<OrderCancelledEvent> notification,
            CancellationToken ct)
        {
            var @event = notification.DomainEvent;
            await InvalidateOrderCache(
                @event.OrderId,
                @event.CustomerId,
                ct);
        }

        // Tập trung logic invalidation vào một chỗ
        private async Task InvalidateOrderCache(
            Guid orderId, Guid customerId,
            CancellationToken ct)
        {
            // Xóa cache order cụ thể
            await cacheService.RemoveAsync(
                CacheKeys.OrderById(orderId), ct);

            // Xóa tất cả cache liên quan đến customer này
            await cacheService.RemoveByPrefixAsync(
                CacheKeys.OrderPrefixForCustomer(customerId), ct);
        }
    }

}
