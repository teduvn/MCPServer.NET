using FluentAssertions;
using NetArchTest.Rules;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OrderManagement.Tests.Architecture
{
    public class LayerDependencyTests
    {
        // Lấy Assembly bằng cách reference một type đặc trưng từ mỗi layer.
        // Nếu project được đổi tên, code này sẽ lỗi compile ngay — intentional.
        private static readonly Assembly DomainAssembly =
            typeof(Order).Assembly;  // từ OrderManagement.Domain


        private static readonly Assembly ApplicationAssembly =
            typeof(PlaceOrderCommand).Assembly;  // từ OrderManagement.Application


        private static readonly Assembly InfrastructureAssembly =
            typeof(ApplicationDbContext).Assembly;  // từ OrderManagement.Infrastructure


        private static readonly Assembly WebApiAssembly =
            typeof(Program).Assembly;  // từ OrderManagement.WebApi

        [Fact]
        public void Domain_ShouldNot_DependOn_Application()
        {
            var result = Types.InAssembly(DomainAssembly)
                .Should()
                .NotHaveDependencyOn("OrderManagement.Application")
                .GetResult();


            result.IsSuccessful.Should().BeTrue(
                because: "Domain Layer không được biết Application tồn tại");
        }


        [Fact]
        public void Domain_ShouldNot_DependOn_Infrastructure()
        {
            var result = Types.InAssembly(DomainAssembly)
                .Should()
                .NotHaveDependencyOn("OrderManagement.Infrastructure")
                .GetResult();


            result.IsSuccessful.Should().BeTrue(
                because: "Domain không được reference EF Core hay bất cứ thứ gì từ Infrastructure");
        }


        [Fact]
        public void Domain_ShouldNot_DependOn_WebApi()
        {
            var result = Types.InAssembly(DomainAssembly)
                .Should()
                .NotHaveDependencyOn("OrderManagement.WebAPI")
                .GetResult();


            result.IsSuccessful.Should().BeTrue();
        }


        // ── Application Layer: chỉ được phụ thuộc vào Domain ──


        [Fact]
        public void Application_ShouldNot_DependOn_Infrastructure()
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .Should()
                .NotHaveDependencyOn("OrderManagement.Infrastructure")
                .GetResult();


            result.IsSuccessful.Should().BeTrue(
                because: "Application chỉ biết interface, không biết EF Core implementation");
        }


        [Fact]
        public void Application_ShouldNot_DependOn_WebApi()
        {
            var result = Types.InAssembly(ApplicationAssembly)
                .Should()
                .NotHaveDependencyOn("OrderManagement.WebAPI")
                .GetResult();


            result.IsSuccessful.Should().BeTrue();
        }


        // ── Infrastructure Layer: không được phụ thuộc vào WebApi ──


        [Fact]
        public void Infrastructure_ShouldNot_DependOn_WebApi()
        {
            var result = Types.InAssembly(InfrastructureAssembly)
                .Should()
                .NotHaveDependencyOn("OrderManagement.WebAPI")
                .GetResult();


            result.IsSuccessful.Should().BeTrue();
        }

    }

}
