using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Common.Interfaces;
using Stripe;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Payment
{
    public sealed class StripePaymentGateway : IPaymentGateway
    {
        private readonly PaymentIntentService _paymentIntentService;
        private readonly RefundService _refundService;
        private readonly ILogger<StripePaymentGateway> _logger;

        public StripePaymentGateway(
            PaymentIntentService paymentIntentService,
            RefundService refundService,
            ILogger<StripePaymentGateway> logger)
        {
            _paymentIntentService = paymentIntentService;
            _refundService = refundService;
            _logger = logger;
        }

        public async Task<PaymentResult> ChargeAsync(
            PaymentRequest request, CancellationToken ct = default)
        {
            // QUAN TRỌNG: IdempotencyKey — cùng key sẽ trả cùng kết quả
            // Tránh double-charge khi network timeout rồi retry
            var idempotencyKey = $"order-{request.OrderId}-{request.Amount}";

            var options = new PaymentIntentCreateOptions
            {
                // Stripe tính bằng cent/xu — nhân 100
                Amount = (long)(request.Amount * 100),
                Currency = request.Currency.ToLower(),  // "vnd"
                PaymentMethod = request.PaymentMethodToken,
                Confirm = true,
                Metadata = new Dictionary<string, string>
                {
                    ["order_id"] = request.OrderId.ToString()
                }
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            try
            {
                var intent = await _paymentIntentService.CreateAsync(
                    options, requestOptions, ct);

                _logger.LogInformation(
                    "Payment {Status} for order {OrderId}: {PaymentIntentId}",
                    intent.Status, request.OrderId, intent.Id);

                return intent.Status == "succeeded"
                    ? new PaymentResult(true, intent.Id, null, null)
                    : new PaymentResult(false, intent.Id, "PAYMENT_FAILED", $"Payment status: {intent.Status}");
            }
            catch (StripeException ex) when (ex.StripeError?.Type == "card_error")
            {
                // Card bị từ chối — business error, KHÔNG retry
                _logger.LogWarning(
                    "Card declined for order {OrderId}: {ErrorCode}",
                    request.OrderId, ex.StripeError.Code);

                return new PaymentResult(
                    false,
                    null,
                    ex.StripeError.Code,
                    ex.StripeError.Message);
            }
            catch (StripeException ex)
            {
                // Network, server error — log và bubble up để Polly retry
                _logger.LogError(ex,
                    "Stripe error for order {OrderId}: {ErrorType}",
                    request.OrderId, ex.StripeError?.Type);

                return new PaymentResult(
                    false,
                    null,
                    ex.StripeError?.Code ?? "STRIPE_ERROR",
                    ex.Message);
            }
        }

        public async Task<PaymentResult> RefundAsync(
            string transactionId, decimal amount, CancellationToken ct = default)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = transactionId,
                    Amount = (long)(amount * 100)
                };

                var refund = await _refundService.CreateAsync(options, null, ct);

                _logger.LogInformation(
                    "Refund {Status} for transaction {TransactionId}: {RefundId}",
                    refund.Status, transactionId, refund.Id);

                return refund.Status == "succeeded"
                    ? new PaymentResult(true, refund.Id, null, null)
                    : new PaymentResult(false, refund.Id, "REFUND_FAILED", $"Refund status: {refund.Status}");
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Refund error for transaction {TransactionId}",
                    transactionId);

                return new PaymentResult(
                    false,
                    null,
                    ex.StripeError?.Code ?? "REFUND_ERROR",
                    ex.Message);
            }
        }
    }

}
