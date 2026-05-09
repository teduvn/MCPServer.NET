using Microsoft.Extensions.Logging;
using OrderManagement.Application.Common.Interfaces;

namespace OrderManagement.Infrastructure.Services
{
    public sealed class InventoryService : IInventoryService
    {
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(ILogger<InventoryService> logger)
        {
            _logger = logger;
        }

        public async Task DeductAsync(Guid productId, int quantity, CancellationToken cancellationToken)
        {
            // TODO: Implement actual inventory deduction logic
            // This could integrate with an external inventory management system
            // For now, log the action
            _logger.LogInformation(
                "Deducting {Quantity} units of product {ProductId} from inventory",
                quantity, productId);

            await Task.CompletedTask;
        }
    }
}
