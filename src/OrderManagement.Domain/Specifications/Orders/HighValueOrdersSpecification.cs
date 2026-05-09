using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Specifications.Orders
{
    // Order có giá trị cao hơn ngưỡng nhất định
    public sealed class HighValueOrdersSpecification : BaseSpecification<Order>
    {
        public HighValueOrdersSpecification(decimal minAmount, string currency = "VND")
            : base(order => order.TotalAmount.Amount >= minAmount
                         && order.TotalAmount.Currency == currency)
        {
            ApplyOrderByDescending(o => o.TotalAmount.Amount);
        }
    }

}
