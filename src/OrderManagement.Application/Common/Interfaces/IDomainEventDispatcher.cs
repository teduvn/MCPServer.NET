using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IDomainEventDispatcher
    {
        Task DispatchEventsAsync(
            IEnumerable<Entity> entities,
            CancellationToken cancellationToken = default);
    }

}
