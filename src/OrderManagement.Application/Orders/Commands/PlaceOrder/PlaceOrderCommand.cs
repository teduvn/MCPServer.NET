using MediatR;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Orders.Commands.PlaceOrder
{
    public class PlaceOrderCommand : IRequest<Result<Guid>>, ITransactionalCommand
    {
        public Guid CustomerId { get; set; }
        public AddressDto ShippingAddress { get; set; } = null!;
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
