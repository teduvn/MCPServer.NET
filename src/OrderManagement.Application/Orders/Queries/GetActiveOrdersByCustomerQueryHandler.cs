using MediatR;
using OrderManagement.Application.Common.Constants;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Mappings;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.Specifications.Orders;

namespace OrderManagement.Application.Orders.Queries
{
    public class GetActiveOrdersByCustomerQuery : IRequest<IReadOnlyList<OrderDto>>
    {
        public Guid CustomerId { get; init; }
    }

    public class GetActiveOrdersByCustomerQueryHandler(
        IOrderRepository orderRepository,
        ICacheService cacheService)
        : IRequestHandler<GetActiveOrdersByCustomerQuery, IReadOnlyList<OrderDto>>
    {
        public async Task<IReadOnlyList<OrderDto>> Handle(
            GetActiveOrdersByCustomerQuery request,
            CancellationToken ct)
        {
            var cacheKey = CacheKeys.OrdersByCustomer(request.CustomerId);
            // Bước 1: Check cache
            var cached = await cacheService.GetAsync<IReadOnlyList<OrderSummaryDto>>(cacheKey, ct);
            if (cached is not null)
            {
                // Cache HIT — trả về ngay, không đụng DB
                return (IReadOnlyList<OrderDto>)cached;
            }

            // Bước 2: Specification được khởi tạo với tham số cụ thể
            var spec = new ActiveOrdersByCustomerSpecification(request.CustomerId);

            // Bước 3: Repository không biết gì về logic filter — chỉ áp Specification
            var orders = await orderRepository.ListAsync(spec, ct);

            // Map sang DTO (bài 3 sẽ đi sâu vào phần này)
            var dtos = orders.Select(o => o.ToDto()).ToList().AsReadOnly();

            // Bước 4: Set cache để request sau dùng lại
            // TTL null = dùng default từ CacheSettings (30 phút)
            await cacheService.SetAsync(cacheKey, dtos, expiry: null, ct);

            return dtos.AsReadOnly();

        }
    }

}
