using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Orders.DTOs
{
    public class OrderSummaryDto
    {
        public Guid Id { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public DateTime OrderDate { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int ItemCount { get; init; }

    }
}
