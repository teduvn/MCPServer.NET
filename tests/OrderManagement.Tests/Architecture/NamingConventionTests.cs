using FluentAssertions;
using FluentValidation;
using MediatR;
using NetArchTest.Rules;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Specifications;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OrderManagement.Tests.Architecture
{
    public class NamingConventionTests
    {
        private static readonly Assembly ApplicationAssembly = typeof(PlaceOrderCommand).Assembly;


        [Fact]
        public void CommandHandlers_ShouldEndWith_Handler()
        {
            // Tất cả class implement IRequestHandler và tên chứa 'Command' phải kết thúc bằng 'Handler'
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .ImplementInterface(typeof(IRequestHandler<,>))
                .And()
                .HaveNameMatching("Command")
                .Should()
                .HaveNameEndingWith("Handler")
                .GetResult();


            result.IsSuccessful.Should().BeTrue(
                because: "Naming convention: command handlers phải kết thúc bằng 'Handler'");
        }


        [Fact]
        public void Validators_ShouldEndWith_Validator()
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .That()
                .Inherit(typeof(AbstractValidator<>))
                .Should()
                .HaveNameEndingWith("Validator")
                .GetResult();


            result.IsSuccessful.Should().BeTrue();
        }


        [Fact]
        public void Specifications_ShouldEndWith_Specification()
        {
            var domainAssembly = typeof(Order).Assembly;


            var result = Types.InAssembly(domainAssembly)
                .That()
                .Inherit(typeof(BaseSpecification<>))
                .Should()
                .HaveNameEndingWith("Specification")
                .GetResult();


            result.IsSuccessful.Should().BeTrue(
                because: "Tất cả Specification class phải kết thúc bằng 'Specification'");
        }
    }

}
