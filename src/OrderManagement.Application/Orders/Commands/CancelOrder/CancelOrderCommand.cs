using MediatR;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Orders.Commands.CancelOrder
{
    public sealed record CancelOrderCommand(Guid OrderId, string? Reason = null)
        : IRequest<Result<Unit>>, ITransactionalCommand;
}
