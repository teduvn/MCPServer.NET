using MediatR;
using OrderManagement.Application.Common.Events;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Common;

namespace OrderManagement.Infrastructure.Persistence
{
    internal sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IPublisher _publisher;

        public MediatRDomainEventDispatcher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task DispatchEventsAsync(
            IEnumerable<Entity> entities,
            CancellationToken cancellationToken = default)
        {
            var domainEvents = entities
                .SelectMany(e => e.DomainEvents)
                .ToList();

            // Clear trước khi dispatch để tránh vòng lặp
            foreach (var entity in entities)
                entity.ClearDomainEvents();

            // Wrap IDomainEvent thành INotification
            foreach (var domainEvent in domainEvents)
            {
                var notificationWrapper = Activator.CreateInstance(
                    typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                    domainEvent);

                await _publisher.Publish(notificationWrapper!, cancellationToken);
            }
        }
    }

}
