using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Domain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDomainServices(
           this IServiceCollection services)

        {
            services.AddTransient<OrderPricingService>();
            return services;
        }
    }
}
    

