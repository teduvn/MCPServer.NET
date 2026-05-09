using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Application.Orders.Commands.UpdateOrder
{
    public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, Result<Unit>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOrderCommandHandler(
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Unit>> Handle(
         UpdateOrderCommand command, CancellationToken ct)
        {
            var order = await _orderRepository.GetByIdAsync(command.OrderId, ct);
            if (order is null)
                return Result<Unit>.Failure(Error.Create("Order.NotFound", "Order không tồn tại"));

            var address = new Address(
                command.NewAddress.Street,
                command.NewAddress.City,
                command.NewAddress.Province,
                command.NewAddress.Country,
                command.NewAddress.PostalCode);

            order.UpdateShippingAddress(address);


            _orderRepository.Update(order);


            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
                return Result<Unit>.Success(Unit.Value);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Có người khác đã sửa Order này sau khi bạn load nó
                // Business decision: throw lỗi để client biết và thử lại
                return Result<Unit>.Failure(
                    Error.Create("Order.Conflict", "Đơn hàng vừa được sửa bởi người khác. Vui lòng tải lại."));
            }
        }
    }

}
