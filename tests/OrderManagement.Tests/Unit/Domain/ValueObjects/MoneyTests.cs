using FluentAssertions;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Tests.Unit.Domain.ValueObjects
{
    public class MoneyTests
    {
        [Fact]
        public void Money_WithSameValues_AreEqual()
        {
            var money1 = Money.FromVND(100_000);
            var money2 = Money.FromVND(100_000);

            money1.Should().Be(money2);  // Value Object equality
        }

        [Fact]
        public void Money_Add_WithSameCurrency_ReturnsCorrectSum()
        {
            var price = Money.FromVND(450_000);
            var shipping = Money.FromVND(30_000);

            var total = price.Add(shipping);

            total.Amount.Should().Be(480_000);
            total.Currency.Should().Be("VND");
        }

        [Fact]
        public void Money_Add_WithDifferentCurrency_ThrowsDomainException()
        {
            var vnd = Money.FromVND(100_000);
            var usd = Money.FromUSD(10);

            var act = () => vnd.Add(usd);

            act.Should().Throw<DomainException>()
               .WithMessage("*VND*USD*");
        }

        [Fact]
        public void Money_Create_WithNegativeAmount_ThrowsDomainException()
        {
            var act = () => Money.Create(-100, "VND");

            act.Should().Throw<DomainException>();
        }
    }

}
