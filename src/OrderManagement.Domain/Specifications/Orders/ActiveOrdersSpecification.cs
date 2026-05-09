using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Specifications.Orders
{
    /// <summary>
    /// Specification: Order đang active = Pending hoặc Confirmed.
    /// Một lần viết, dùng được ở mọi nơi cần filter active orders.
    /// </summary>
    public sealed class ActiveOrdersSpecification : BaseSpecification<Order>
    {
        public ActiveOrdersSpecification()
            : base(order => order.Status == OrderStatus.Placed
                         || order.Status == OrderStatus.Confirmed)
        {
            // Include OrderItems khi lấy active orders (thường cần hiển thị)
            AddInclude(o => o.Items);
            ApplyOrderByDescending(o => o.CreatedAt);
        }
    }

}
