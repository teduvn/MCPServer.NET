using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.ValueObjects
{
    public sealed record Money(decimal Amount, string Currency)
    {
        // Validation ngay tại constructor — Money không thể tồn tại ở trạng thái invalid
        public static Money Create(decimal amount, string currency)
        {
            if (amount < 0)
                throw new DomainException("Số tiền không thể âm.");

            if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
                throw new DomainException("Currency code phải là 3 ký tự theo chuẩn ISO 4217.");

            return new Money(amount, currency.ToUpperInvariant());
        }

        // Các phép tính — trả về Money mới, không mutate
        public Money Add(Money other)
        {
            EnsureSameCurrency(other);
            return this with { Amount = Amount + other.Amount };
        }

        public Money Subtract(Money other)
        {
            EnsureSameCurrency(other);
            var result = Amount - other.Amount;
            if (result < 0)
                throw new DomainException("Kết quả phép trừ không thể âm.");
            return this with { Amount = result };
        }

        public Money Multiply(decimal factor)
        {
            if (factor < 0)
                throw new DomainException("Hệ số nhân không thể âm.");
            return this with { Amount = Math.Round(Amount * factor, 2) };
        }

        // Operator overloading
        public static Money operator +(Money left, Money right) => left.Add(right);
        public static Money operator -(Money left, Money right) => left.Subtract(right);
        public static Money operator *(Money money, decimal factor) => money.Multiply(factor);

        // Helper constants
        public static Money Zero => new(0, "VND");
        public static Money ZeroOf(string currency) => new(0, currency);
        public static Money FromVND(decimal amount) => Create(amount, "VND");
        public static Money FromUSD(decimal amount) => Create(amount, "USD");

        private void EnsureSameCurrency(Money other)
        {
            if (Currency != other.Currency)
                throw new DomainException($"Không thể cộng {Currency} với {other.Currency}.");
        }

        public override string ToString() => $"{Amount:N2} {Currency}";
    }

}
