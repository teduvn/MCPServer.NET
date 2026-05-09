using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Common.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OrderManagement.Infrastructure.Email
{
    public sealed class SendGridEmailService : IEmailService
    {
        private readonly ISendGridClient _client;
        private readonly SendGridSettings _options;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(
            ISendGridClient client,
            IOptions<SendGridSettings> options,
            ILogger<SendGridEmailService> logger)
        {
            _client = client;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(
            string toEmail,
            string customerName,
            Guid orderId,
            decimal totalAmount,
            CancellationToken cancellationToken = default)
        {
            var from = new EmailAddress(_options.SenderEmail, _options.SenderName);
            var to = new EmailAddress(toEmail, customerName);
            var subject = $"Xác nhận đơn hàng #{orderId}";

            // Build HTML content — production nên dùng template engine
            var htmlContent = BuildOrderConfirmationHtml(customerName, orderId, totalAmount);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

            // Thêm category để tracking trong SendGrid dashboard
            msg.AddCategory("order-confirmation");

            var response = await _client.SendEmailAsync(msg, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Body != null 
                    ? await response.Body.ReadAsStringAsync(cancellationToken)
                    : string.Empty;
                _logger.LogError(
                    "SendGrid failed for order {OrderId}: {StatusCode} - {Body}",
                    orderId, response.StatusCode, body);

                // Throw để Polly retry (xem section 4)
                throw new EmailDeliveryException(
                    $"Failed to send order confirmation: {response.StatusCode}");
            }

            _logger.LogInformation(
                "Order confirmation sent for order {OrderId} to {Email}",
                orderId, toEmail);
        }

        public async Task SendShippingNotificationAsync(
            string toEmail,
            string customerName,
            Guid orderId,
            string trackingNumber,
            CancellationToken cancellationToken = default)
        {
            var from = new EmailAddress(_options.SenderEmail, _options.SenderName);
            var to = new EmailAddress(toEmail, customerName);
            var subject = $"Đơn hàng #{orderId} đã được giao cho vận chuyển";

            var htmlContent = BuildShippingNotificationHtml(customerName, orderId, trackingNumber);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
            msg.AddCategory("shipping-notification");

            var response = await _client.SendEmailAsync(msg, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Body != null 
                    ? await response.Body.ReadAsStringAsync(cancellationToken)
                    : string.Empty;
                _logger.LogError(
                    "SendGrid failed for shipping notification {OrderId}: {StatusCode} - {Body}",
                    orderId, response.StatusCode, body);

                throw new EmailDeliveryException(
                    $"Failed to send shipping notification: {response.StatusCode}");
            }

            _logger.LogInformation(
                "Shipping notification sent for order {OrderId} to {Email}",
                orderId, toEmail);
        }

        private static string BuildOrderConfirmationHtml(string customerName, Guid orderId, decimal totalAmount)
        {
            // Simplified — production dùng Razor template hoặc Fluid
            return $"""
            <h2>Xin chào {customerName},</h2>
            <h3>Xác nhận đơn hàng #{orderId}</h3>
            <p>Cảm ơn bạn đã đặt hàng. Đơn hàng của bạn đang được xử lý.</p>
            <p><strong>Tổng tiền: {totalAmount:C}</strong></p>
            """;
        }

        private static string BuildShippingNotificationHtml(string customerName, Guid orderId, string trackingNumber)
        {
            return $"""
            <h2>Xin chào {customerName},</h2>
            <h3>Đơn hàng #{orderId} đã được giao cho vận chuyển</h3>
            <p>Đơn hàng của bạn đang trên đường giao đến.</p>
            <p><strong>Mã vận đơn: {trackingNumber}</strong></p>
            <p>Bạn có thể theo dõi đơn hàng bằng mã vận đơn trên.</p>
            """;
        }
    }


}
