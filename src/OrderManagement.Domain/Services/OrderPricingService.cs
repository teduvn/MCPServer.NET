using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Services
{
    /// <summary>
    /// Domain Service tính giá đơn hàng.
    /// Stateless — không có field, chỉ nhận input qua method parameter.
    /// </summary>
    public sealed class OrderPricingService
    {
        // Không inject gì — domain service phải stateless

        /// <summary>
        /// Tính giá cuối cùng của đơn hàng.
        /// Tất cả input cần thiết được truyền vào method — không có global state.
        /// </summary>
        public Money CalculateFinalPrice(
            Order order,
            CustomerTier customerTier,
            ShippingZone shippingZone,
            Voucher? voucher = null)
        {
            // 1. Tính subtotal từ các OrderItem
            var subtotal = order.Items
                .Aggregate(Money.Zero, (acc, item) => acc + item.Subtotal);

            // 2. Áp dụng discount theo CustomerTier
            var discountRate = GetDiscountRate(customerTier);
            var discountAmount = subtotal * discountRate;
            var priceAfterDiscount = subtotal - discountAmount;

            // 3. Tính phí vận chuyển
            var shippingFee = CalculateShippingFee(order, shippingZone);

            // 4. Áp dụng voucher nếu có
            var voucherDiscount = voucher?.Apply(priceAfterDiscount) ?? Money.Zero;

            // 5. Cộng lại
            return priceAfterDiscount + shippingFee - voucherDiscount;
        }

        private static decimal GetDiscountRate(CustomerTier tier) => tier switch
        {
            CustomerTier.Silver => 0.05m,  // 5%
            CustomerTier.Gold => 0.10m,  // 10%
            CustomerTier.Platinum => 0.15m,  // 15%
            _ => 0m       // Standard — không có discount
        };

        private static Money CalculateShippingFee(Order order, ShippingZone zone)
        {
            // Logic tính phí vận chuyển theo zone và trọng lượng
            var totalWeight = order.Items.Sum(i => i.Product.WeightKg * i.Quantity);
            var baseRate = zone.RatePerKg;
            return new Money(totalWeight * baseRate, order.Currency);
        }
    }


}
