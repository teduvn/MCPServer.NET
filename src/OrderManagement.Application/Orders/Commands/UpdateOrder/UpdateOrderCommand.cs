using MediatR;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Orders.Commands.UpdateOrder
{
    public class UpdateOrderCommand : IRequest<Result<Unit>>, ITransactionalCommand
    {
        public Guid OrderId { get; set; }
        public AddressDto NewAddress { get; set; } = null!;
    }
}
