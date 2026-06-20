using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Repositories;

namespace OrderManagement.Application.Orders.Commands.UpdateOrderStatus
{
    public sealed class UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateOrderStatusCommand, Result<OrderStatus>>
    {
        public async Task<Result<OrderStatus>> Handle(
            UpdateOrderStatusCommand request,
            CancellationToken cancellationToken)
        {
            var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order is null)
            {
                return Result<OrderStatus>.Failure(
                    Error.Create("Order.NotFound", $"Order '{request.OrderId}' was not found."));
            }

            try
            {
                switch (request.NewStatus)
                {
                    case OrderStatus.Placed:
                        order.Place();
                        break;
                    case OrderStatus.Shipped:
                        order.Ship("MCP-MANUAL", DateTime.UtcNow.AddDays(3));
                        break;
                    default:
                        return Result<OrderStatus>.Failure(
                            Error.Create(
                                "Order.InvalidStatusTransition",
                                $"Status '{request.NewStatus}' is not supported by the current workflow."));
                }

                orderRepository.Update(order);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<OrderStatus>.Success(order.Status);
            }
            catch (DomainException ex)
            {
                return Result<OrderStatus>.Failure(
                    Error.Create("Order.InvalidStatusTransition", ex.Message));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<OrderStatus>.Failure(
                    Error.Create("Order.Conflict", "Order was modified by another user. Please retry."));
            }
        }
    }
}
