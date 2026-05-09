using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Common
{
    public interface IDomainEvent
    {
        Guid Id { get; }
        DateTime OccurredOn { get; }
    }
}
