using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Services;
using OrderManagement.Domain.ValueObjects;
using System;
using Xunit;

namespace OrderManagement.Tests.Unit.Domain.Services
{
    public class OrderPricingServiceTests
    {
        private readonly OrderPricingService _service;

        public OrderPricingServiceTests()
        {
            _service = new OrderPricingService();
        }

        #region CustomerTier Discount Tests

        [Theory]
        [InlineData(CustomerTier.Standard, 0)]
        [InlineData(CustomerTier.Silver, 0.05)]
        [InlineData(CustomerTier.Gold, 0.10)]
        [InlineData(CustomerTier.Platinum, 0.15)]
        public void CalculateFinalPrice_Should_Apply_Correct_Discount_By_CustomerTier(
            CustomerTier tier, decimal expectedDiscountRate)
        {
            // Arrange
            var product = CreateTestProduct(100_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 2);
            var subtotal = 200_000m; // 100k * 2
            var expectedAfterDiscount = subtotal * (1 - expectedDiscountRate);
            var expectedShipping = 2.0m * 2 * 15_000m; // weight * quantity * rate
            var expectedTotal = expectedAfterDiscount + expectedShipping;

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                tier,
                ShippingZone.Domestic,
                null);

            // Assert
            Assert.Equal(expectedTotal, finalPrice.Amount);
            Assert.Equal("VND", finalPrice.Currency);
        }

        #endregion

        #region Voucher Tests

        [Fact]
        public void CalculateFinalPrice_With_Percentage_Voucher_Should_Apply_Discount()
        {
            // Arrange
            var product = CreateTestProduct(100_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 2);
            var voucher = CreateVoucher(VoucherType.Percentage, 20m); // 20% discount

            var subtotal = 200_000m;
            var tierDiscount = subtotal * 0.10m; // Gold = 10%
            var afterTierDiscount = subtotal - tierDiscount; // 180,000
            var shipping = 60_000m; // 2kg * 2 * 15k
            var voucherDiscount = afterTierDiscount * 0.20m; // 36,000
            var expected = afterTierDiscount + shipping - voucherDiscount; // 204,000

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Gold,
                ShippingZone.Domestic,
                voucher);

            // Assert
            Assert.Equal(expected, finalPrice.Amount);
        }

        [Fact]
        public void CalculateFinalPrice_With_FixedAmount_Voucher_Should_Apply_Discount()
        {
            // Arrange
            var product = CreateTestProduct(100_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 2);
            var voucher = CreateVoucher(VoucherType.FixedAmount, 50_000m);

            var subtotal = 200_000m;
            var tierDiscount = subtotal * 0.10m; // Gold = 10%
            var afterTierDiscount = subtotal - tierDiscount; // 180,000
            var shipping = 60_000m;
            var voucherDiscount = 50_000m;
            var expected = afterTierDiscount + shipping - voucherDiscount; // 190,000

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Gold,
                ShippingZone.Domestic,
                voucher);

            // Assert
            Assert.Equal(expected, finalPrice.Amount);
        }

        [Fact]
        public void CalculateFinalPrice_With_Voucher_MaxDiscount_Should_Cap_Discount()
        {
            // Arrange
            var product = CreateTestProduct(500_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 2);
            var maxDiscount = Money.FromVND(100_000m);
            var voucher = Voucher.Create(
                "SAVE50",
                VoucherType.Percentage,
                50m, // 50% discount
                null,
                maxDiscount, // Cap at 100k
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(30),
                100);

            var subtotal = 1_000_000m;
            var tierDiscount = subtotal * 0.10m; // Gold = 10%
            var afterTierDiscount = subtotal - tierDiscount; // 900,000
            var shipping = 60_000m;
            var voucherDiscount = 100_000m; // Capped
            var expected = afterTierDiscount + shipping - voucherDiscount; // 860,000

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Gold,
                ShippingZone.Domestic,
                voucher);

            // Assert
            Assert.Equal(expected, finalPrice.Amount);
        }

        [Fact]
        public void CalculateFinalPrice_With_Voucher_MinOrderValue_Below_Threshold_Should_Throw()
        {
            // Arrange
            var product = CreateTestProduct(50_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 1);
            var minOrder = Money.FromVND(100_000m);
            var voucher = Voucher.Create(
                "VIP100",
                VoucherType.Percentage,
                10m,
                minOrder, // Minimum 100k
                null,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(30),
                100);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                _service.CalculateFinalPrice(
                    order,
                    CustomerTier.Standard,
                    ShippingZone.Domestic,
                    voucher));

            Assert.Contains("tối thiểu", exception.Message.ToLower());
        }

        [Fact]
        public void CalculateFinalPrice_With_Expired_Voucher_Should_Throw()
        {
            // Arrange
            var product = CreateTestProduct(100_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 2);
            var voucher = Voucher.Create(
                "EXPIRED",
                VoucherType.Percentage,
                10m,
                null,
                null,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(-1), // Expired yesterday
                100);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                _service.CalculateFinalPrice(
                    order,
                    CustomerTier.Standard,
                    ShippingZone.Domestic,
                    voucher));

            Assert.Contains("hiệu lực", exception.Message.ToLower());
        }

        [Fact]
        public void CalculateFinalPrice_With_Inactive_Voucher_Should_Throw()
        {
            // Arrange
            var product = CreateTestProduct(100_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 2);
            var voucher = CreateVoucher(VoucherType.Percentage, 10m);
            voucher.Deactivate();

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                _service.CalculateFinalPrice(
                    order,
                    CustomerTier.Standard,
                    ShippingZone.Domestic,
                    voucher));

            Assert.Contains("hoạt động", exception.Message.ToLower());
        }

        [Fact]
        public void CalculateFinalPrice_Without_Voucher_Should_Work()
        {
            // Arrange
            var product = CreateTestProduct(100_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 2);

            var subtotal = 200_000m;
            var shipping = 60_000m;
            var expected = subtotal + shipping; // 260,000

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Standard,
                ShippingZone.Domestic,
                null);

            // Assert
            Assert.Equal(expected, finalPrice.Amount);
        }

        #endregion

        #region ShippingZone Tests

        [Theory]
        [InlineData("DOMESTIC", 15_000)]
        [InlineData("REGIONAL", 25_000)]
        [InlineData("INTL", 80_000)]
        public void CalculateFinalPrice_Should_Calculate_Correct_Shipping_By_Zone(
            string zoneCode, decimal ratePerKg)
        {
            // Arrange
            var product = CreateTestProduct(100_000m, 3.5m); // 3.5kg
            var order = CreateOrderWithProduct(product, quantity: 2);
            var zone = GetShippingZone(zoneCode);

            var subtotal = 200_000m;
            var expectedShipping = 3.5m * 2 * ratePerKg;
            var expected = subtotal + expectedShipping;

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Standard,
                zone,
                null);

            // Assert
            Assert.Equal(expected, finalPrice.Amount);
        }

        #endregion

        #region Multiple Items Tests

        [Fact]
        public void CalculateFinalPrice_With_Multiple_Products_Should_Calculate_Correctly()
        {
            // Arrange
            var product1 = CreateTestProduct(100_000m, 2.0m);
            var product2 = CreateTestProduct(150_000m, 3.5m);
            var product3 = CreateTestProduct(50_000m, 1.0m);

            var address = Address.Create("123 Test St", "Hanoi", "Hanoi", "VN");
            var orderResult = Order.CreateDraft(Guid.NewGuid(), address);
            var order = orderResult.Value;

            order.AddItem(product1.Id, product1.Name, product1.Price, 2);
            order.AddItem(product2.Id, product2.Name, product2.Price, 1);
            order.AddItem(product3.Id, product3.Name, product3.Price, 3);

            // Manually set Product navigation for each item (simulating EF Core)
            foreach (var item in order.Items)
            {
                if (item.ProductId == product1.Id)
                    SetProductNavigation(item, product1);
                else if (item.ProductId == product2.Id)
                    SetProductNavigation(item, product2);
                else if (item.ProductId == product3.Id)
                    SetProductNavigation(item, product3);
            }

            var voucher = CreateVoucher(VoucherType.Percentage, 10m);

            var subtotal = (100_000m * 2) + (150_000m * 1) + (50_000m * 3); // 500,000
            var tierDiscount = subtotal * 0.05m; // Silver = 5% = 25,000
            var afterTierDiscount = subtotal - tierDiscount; // 475,000
            var totalWeight = (2.0m * 2) + (3.5m * 1) + (1.0m * 3); // 10.5kg
            var shipping = totalWeight * 15_000m; // 157,500
            var voucherDiscount = afterTierDiscount * 0.10m; // 47,500
            var expected = afterTierDiscount + shipping - voucherDiscount; // 585,000

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Silver,
                ShippingZone.Domestic,
                voucher);

            // Assert
            Assert.Equal(expected, finalPrice.Amount);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void CalculateFinalPrice_With_Empty_Order_Should_Return_Only_Shipping()
        {
            // Arrange
            var address = Address.Create("123 Test St", "Hanoi", "Hanoi", "VN");
            var orderResult = Order.CreateDraft(Guid.NewGuid(), address);
            var order = orderResult.Value;

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Standard,
                ShippingZone.Domestic,
                null);

            // Assert
            Assert.Equal(0m, finalPrice.Amount); // No items, no weight, no shipping
        }

        [Fact]
        public void CalculateFinalPrice_With_High_Tier_And_Voucher_Should_Stack_Discounts()
        {
            // Arrange
            var product = CreateTestProduct(1_000_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 1);
            var voucher = CreateVoucher(VoucherType.FixedAmount, 100_000m);

            var subtotal = 1_000_000m;
            var tierDiscount = subtotal * 0.15m; // Platinum = 15% = 150,000
            var afterTierDiscount = subtotal - tierDiscount; // 850,000
            var shipping = 30_000m; // 2kg * 15k
            var voucherDiscount = 100_000m;
            var expected = afterTierDiscount + shipping - voucherDiscount; // 780,000

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Platinum,
                ShippingZone.Domestic,
                voucher);

            // Assert
            Assert.Equal(expected, finalPrice.Amount);
        }

        [Fact]
        public void CalculateFinalPrice_International_Shipping_Should_Be_Most_Expensive()
        {
            // Arrange
            var product = CreateTestProduct(100_000m, 2.0m);
            var order = CreateOrderWithProduct(product, quantity: 1);

            var subtotal = 100_000m;
            var shipping = 2.0m * 80_000m; // 160,000
            var expected = subtotal + shipping; // 260,000

            // Act
            var finalPrice = _service.CalculateFinalPrice(
                order,
                CustomerTier.Standard,
                ShippingZone.International,
                null);

            // Assert
            Assert.Equal(expected, finalPrice.Amount);
            Assert.True(finalPrice.Amount > 200_000m); // Verify it's expensive
        }

        #endregion

        #region Helper Methods

        private Product CreateTestProduct(decimal price, decimal weightKg)
        {
            return Product.Create(
                $"Product-{Guid.NewGuid()}",
                "Test Description",
                Money.FromVND(price),
                weightKg,
                100);
        }

        private Order CreateOrderWithProduct(Product product, int quantity)
        {
            var address = Address.Create("123 Test St", "Hanoi", "Hanoi", "VN");
            var orderResult = Order.CreateDraft(Guid.NewGuid(), address);
            var order = orderResult.Value;
            order.AddItem(product.Id, product.Name, product.Price, quantity);

            // Simulate EF Core navigation property population
            foreach (var item in order.Items)
            {
                SetProductNavigation(item, product);
            }

            return order;
        }

        private Voucher CreateVoucher(VoucherType type, decimal discountValue)
        {
            return Voucher.Create(
                $"VOUCHER-{Guid.NewGuid().ToString().Substring(0, 8)}",
                type,
                discountValue,
                null,
                null,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(30),
                100);
        }

        private ShippingZone GetShippingZone(string code)
        {
            return code switch
            {
                "DOMESTIC" => ShippingZone.Domestic,
                "REGIONAL" => ShippingZone.Regional,
                "INTL" => ShippingZone.International,
                _ => ShippingZone.Domestic
            };
        }

        private void SetProductNavigation(OrderItem item, Product product)
        {
            // Use reflection to set the private Product property
            var productProperty = typeof(OrderItem).GetProperty("Product");
            productProperty?.SetValue(item, product);
        }

        #endregion
    }
}
