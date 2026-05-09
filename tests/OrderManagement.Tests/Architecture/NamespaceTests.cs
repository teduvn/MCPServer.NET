using FluentAssertions;
using NetArchTest.Rules;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OrderManagement.Tests.Architecture
{
    public class NamespaceTests
    {
        private static readonly Assembly DomainAssembly = typeof(Order).Assembly;
        private static readonly Assembly ApplicationAssembly = typeof(PlaceOrderCommand).Assembly;


        [Fact]
        public void DomainEntities_ShouldBe_InCorrectNamespace()
        {
            // Entities trong Domain phải nằm trong namespace OrderManagement.Domain.*
            var result = Types.InAssembly(DomainAssembly)
                .That()
                .Inherit(typeof(Entity))
                .Should()
                .ResideInNamespace("OrderManagement.Domain")
                .GetResult();


            result.IsSuccessful.Should().BeTrue();
        }


        [Fact]
        public void DomainEvents_ShouldBe_InEventsNamespace()
        {
            // Domain Events phải nằm trong namespace chứa 'Events'
            var result = Types.InAssembly(DomainAssembly)
                .That()
                .ImplementInterface(typeof(IDomainEvent))
                .Should()
                .ResideInNamespaceContaining("Events")
                .GetResult();


            result.IsSuccessful.Should().BeTrue(
                because: "Domain events phải nằm trong subfolder Events để dễ tìm");
        }
    }

}
