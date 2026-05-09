using FluentAssertions;
using NetArchTest.Rules;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OrderManagement.Tests.Architecture
{
    public class ThirdPartyDependencyTests
    {
        private static readonly Assembly DomainAssembly = typeof(Order).Assembly;
        private static readonly Assembly ApplicationAssembly = typeof(PlaceOrderCommand).Assembly;


        [Fact]
        public void Domain_ShouldNot_DependOn_EntityFrameworkCore()
        {
            // Domain không được biết EF Core tồn tại
            var result = Types.InAssembly(DomainAssembly)
                .Should()
                .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
                .GetResult();


            result.IsSuccessful.Should().BeTrue(
                because: "Domain Layer phải hoàn toàn độc lập với persistence technology");
        }


        [Fact]
        public void Domain_ShouldNot_DependOn_MediatR()
        {
            // Domain không cần biết MediatR — đó là concern của Application
            var result = Types.InAssembly(DomainAssembly)
                .Should()
                .NotHaveDependencyOn("MediatR")
                .GetResult();


            result.IsSuccessful.Should().BeTrue(
                because: "MediatR là application plumbing, không phải domain concern");
        }


        //[Fact]
        //public void Application_ShouldNot_DependOn_EntityFrameworkCore()
        //{
        //    // Application chỉ được dùng interface IOrderRepository, không phải DbContext
        //    var result = Types.InAssembly(ApplicationAssembly)
        //        .Should()
        //        .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
        //        .GetResult();


        //    result.IsSuccessful.Should().BeTrue(
        //        because: "Application không được biết EF Core — nếu đổi ORM thì Application không đổi gì");
        //}


        [Fact]
        public void Application_ShouldNot_DependOn_StackExchangeRedis()
        {
            // Application chỉ biết ICacheService interface
            var result = Types.InAssembly(ApplicationAssembly)
                .Should()
                .NotHaveDependencyOn("StackExchange.Redis")
                .GetResult();


            result.IsSuccessful.Should().BeTrue();
        }
    }

}
