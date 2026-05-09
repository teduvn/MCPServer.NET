using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    // DTO thuần C# — không import Stripe hay bất kỳ SDK nào
    public record PaymentRequest(
        Guid OrderId,
        decimal Amount,
        string Currency,
        string PaymentMethodToken);  // token từ frontend, không phải card number

    public record PaymentResult(
        bool IsSuccess,
        string? TransactionId,
        string? ErrorCode,
        string? ErrorMessage);

    public interface IPaymentGateway
    {
        Task<PaymentResult> ChargeAsync(
            PaymentRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentResult> RefundAsync(
            string transactionId,
            decimal amount,
            CancellationToken cancellationToken = default);
    }

}
