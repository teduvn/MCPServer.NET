using OrderManagement.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IEmailService
    {
        // Tên method = hành động nghiệp vụ cụ thể
        Task SendOrderConfirmationAsync(
            string toEmail,
            string customerName,
            Guid orderId,
            decimal totalAmount,
            CancellationToken cancellationToken = default);

        Task SendShippingNotificationAsync(
            string toEmail,
            string customerName,
            Guid orderId,
            string trackingNumber,
            CancellationToken cancellationToken = default);

    }
}
