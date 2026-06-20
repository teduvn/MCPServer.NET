using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Specifications.Orders.Dtos;

namespace OrderManagement.Domain.Specifications.Orders
{
    public class OrderSearchSpecification : BaseSpecification<Order>
    {
        public OrderSearchSpecification(OrderSearchFilter filter)
            : base()
        {
            // Chỉ thêm criteria khi có giá trị — null thì bỏ qua
            if (filter.Status.HasValue)
                AddCriteria(o => o.Status == filter.Status.Value);


            if (!string.IsNullOrEmpty(filter.CustomerName))
                AddCriteria(o => o.CustomerEmail.Contains(filter.CustomerName));


            if (filter.MinAmount.HasValue)
                AddCriteria(o => o.TotalAmount.Amount >= filter.MinAmount.Value);


            if (filter.MaxAmount.HasValue)
                AddCriteria(o => o.TotalAmount.Amount <= filter.MaxAmount.Value);


            if (filter.FromDate.HasValue)
                AddCriteria(o => o.CreatedAt >= filter.FromDate.Value);


            if (filter.ToDate.HasValue)
                AddCriteria(o => o.CreatedAt <= filter.ToDate.Value);


            // Luôn include Customer để tránh lazy loading
            AddInclude(o => o.Items);


            // Sắp xếp mặc định: mới nhất lên đầu
            ApplyOrderByDescending(o => o.CreatedAt);


            // Pagination
            if (filter.PageSize > 0)
                ApplyPaging(filter.Skip, filter.PageSize);
        }
    }

}
