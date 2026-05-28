using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Specifications.Orders.Dtos
{
    public record OrderSearchFilter
    {
        public OrderStatus? Status { get; init; }
        public string? CustomerName { get; init; }
        public decimal? MinAmount { get; init; }
        public decimal? MaxAmount { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public int PageSize { get; init; } = 20;  // Default 20 kết quả
        public int Skip { get; init; } = 0;
    }

}
