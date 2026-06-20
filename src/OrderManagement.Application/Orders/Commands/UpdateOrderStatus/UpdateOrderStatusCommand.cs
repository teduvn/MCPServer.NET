using MediatR;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Commands.UpdateOrderStatus
{
    public sealed record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus)
        : IRequest<Result<OrderStatus>>, ITransactionalCommand;
}
