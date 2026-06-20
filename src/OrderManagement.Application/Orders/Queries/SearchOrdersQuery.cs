using MediatR;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.Specifications.Orders;
using OrderManagement.Domain.Specifications.Orders.Dtos;

namespace OrderManagement.Application.Orders.Queries
{
    public record SearchOrdersQuery(OrderSearchFilter Filter) : IRequest<List<OrderSummaryDto>>;


    public class SearchOrdersQueryHandler
        : IRequestHandler<SearchOrdersQuery, List<OrderSummaryDto>>
    {
        private readonly IOrderRepository _repository;


        public SearchOrdersQueryHandler(IOrderRepository repository)
        {
            _repository = repository;
        }


        public async Task<List<OrderSummaryDto>> Handle(
            SearchOrdersQuery request, CancellationToken ct)
        {
            // Tạo Specification từ filter — tái sử dụng Specification Pattern
            var spec = new OrderSearchSpecification(request.Filter);


            // Repository call — không biết gì về EF Core bên dưới
            var orders = await _repository.ListAsync(spec, ct);


            return orders.Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.Id.ToString(),
                OrderDate = o.CreatedAt,
                CustomerId = o.CustomerId,
                CustomerName = o.CustomerEmail,
                TotalAmount = o.TotalAmount.Amount,
                Currency = o.TotalAmount.Currency,
                Status = o.Status.ToString(),
                ItemCount = o.Items.Count
            }).ToList();
        }
    }

}
