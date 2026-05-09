using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IInventoryService
    {
        Task DeductAsync(Guid productId, int quantity, CancellationToken cancellationToken);
    }
}
