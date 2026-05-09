using OrderManagement.Domain.Common;
using OrderManagement.Domain.ValueObjects;
using System;

namespace OrderManagement.Domain.Entities
{
    /// <summary>
    /// Entity Voucher - đại diện cho phiếu giảm giá.
    /// </summary>
    public sealed class Voucher : Entity
    {
        public string Code { get; private set; } = null!;
        public VoucherType Type { get; private set; }
        public decimal DiscountValue { get; private set; }
        public Money? MinimumOrderValue { get; private set; }
        public Money? MaximumDiscountAmount { get; private set; }
        public DateTime ValidFrom { get; private set; }
        public DateTime ValidTo { get; private set; }
        public int UsageLimit { get; private set; }
        public int UsedCount { get; private set; }
        public bool IsActive { get; private set; }

        private Voucher() { }

        public static Voucher Create(
            string code,
            VoucherType type,
            decimal discountValue,
            Money? minimumOrderValue,
            Money? maximumDiscountAmount,
            DateTime validFrom,
            DateTime validTo,
            int usageLimit)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new DomainException("Mã voucher không được để trống.");

            if (discountValue <= 0)
                throw new DomainException("Giá trị giảm giá phải lớn hơn 0.");

            if (type == VoucherType.Percentage && discountValue > 100)
                throw new DomainException("Phần trăm giảm giá không thể vượt quá 100%.");

            if (validFrom >= validTo)
                throw new DomainException("Ngày bắt đầu phải trước ngày kết thúc.");

            if (usageLimit <= 0)
                throw new DomainException("Giới hạn sử dụng phải lớn hơn 0.");

            return new Voucher
            {
                Id = Guid.NewGuid(),
                Code = code.ToUpperInvariant(),
                Type = type,
                DiscountValue = discountValue,
                MinimumOrderValue = minimumOrderValue,
                MaximumDiscountAmount = maximumDiscountAmount,
                ValidFrom = validFrom,
                ValidTo = validTo,
                UsageLimit = usageLimit,
                UsedCount = 0,
                IsActive = true
            };
        }

        /// <summary>
        /// Áp dụng voucher và tính toán số tiền giảm giá.
        /// </summary>
        public Money Apply(Money orderAmount)
        {
            ValidateCanBeUsed(orderAmount);

            var discountAmount = Type switch
            {
                VoucherType.Percentage => orderAmount.Multiply(DiscountValue / 100m),
                VoucherType.FixedAmount => new Money(DiscountValue, orderAmount.Currency),
                _ => throw new DomainException($"Loại voucher {Type} không được hỗ trợ.")
            };

            if (MaximumDiscountAmount != null && discountAmount.Amount > MaximumDiscountAmount.Amount)
            {
                discountAmount = MaximumDiscountAmount;
            }

            if (discountAmount.Amount > orderAmount.Amount)
            {
                discountAmount = orderAmount;
            }

            return discountAmount;
        }

        public void MarkAsUsed()
        {
            if (UsedCount >= UsageLimit)
                throw new DomainException("Voucher đã hết lượt sử dụng.");

            UsedCount++;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        private void ValidateCanBeUsed(Money orderAmount)
        {
            if (!IsActive)
                throw new DomainException("Voucher không còn hoạt động.");

            var now = DateTime.UtcNow;
            if (now < ValidFrom || now > ValidTo)
                throw new DomainException($"Voucher chỉ có hiệu lực từ {ValidFrom:dd/MM/yyyy} đến {ValidTo:dd/MM/yyyy}.");

            if (UsedCount >= UsageLimit)
                throw new DomainException("Voucher đã hết lượt sử dụng.");

            if (MinimumOrderValue != null && orderAmount.Amount < MinimumOrderValue.Amount)
                throw new DomainException($"Giá trị đơn hàng tối thiểu phải là {MinimumOrderValue}.");
        }
    }

    public enum VoucherType
    {
        Percentage = 0,
        FixedAmount = 1
    }
}
