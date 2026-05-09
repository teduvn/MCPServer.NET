using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Specifications.Orders
{
    public sealed class OrdersByCustomerSpecification : BaseSpecification<Order>
    {
        public OrdersByCustomerSpecification(Guid customerId)
            : base(order => order.CustomerId == customerId)
        {
            AddInclude(o => o.Items);
            ApplyOrderByDescending(o => o.CreatedAt);
        }
    }

}
