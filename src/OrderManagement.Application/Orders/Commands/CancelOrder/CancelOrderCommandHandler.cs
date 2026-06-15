using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Repositories;

namespace OrderManagement.Application.Orders.Commands.CancelOrder
{
    public sealed class CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<CancelOrderCommand, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(
            CancelOrderCommand request,
            CancellationToken cancellationToken)
        {
            var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order is null)
                return Result<Unit>.Failure(Error.Create("Order.NotFound", "Order was not found."));

            try
            {
                order.Cancel(request.Reason ?? string.Empty);
                orderRepository.Update(order);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Unit>.Success(Unit.Value);
            }
            catch (DomainException ex)
            {
                return Result<Unit>.Failure(
                    Error.Create("Order.CancelFailed", ex.Message));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<Unit>.Failure(
                    Error.Create("Order.Conflict", "Đơn hàng vừa được sửa bởi người khác. Vui lòng tải lại."));
            }
        }
    }
}
