using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Specifications.Orders
{
    // Kết hợp: active AND thuộc về customer cụ thể
    public sealed class ActiveOrdersByCustomerSpecification : BaseSpecification<Order>
    {
        public ActiveOrdersByCustomerSpecification(Guid customerId)
            : base(order => (order.Status == OrderStatus.Placed
                          || order.Status == OrderStatus.Confirmed)
                         && order.CustomerId == customerId)
        {
            AddInclude(o => o.Items);
            ApplyOrderByDescending(o => o.CreatedAt);
        }
    }

}
