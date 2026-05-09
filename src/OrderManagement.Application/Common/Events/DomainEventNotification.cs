using MediatR;
using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Events
{
    // Wrapper để bridge IDomainEvent sang INotification
    public sealed record DomainEventNotification<TDomainEvent>(
        TDomainEvent DomainEvent) : INotification
        where TDomainEvent : IDomainEvent;
}
