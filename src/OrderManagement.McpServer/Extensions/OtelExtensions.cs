using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderManagement.Application.Common.Observability;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.McpServer.Extensions
{
    public static class OtelExtensions
    {
        public static IServiceCollection AddOmsObservability(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var resource = ResourceBuilder.CreateDefault()
                .AddService(serviceName: "OMS.McpServer", serviceVersion: "1.0.0");


            services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .SetResourceBuilder(resource)
                    // HTTP request/response — đặc thù McpServer host
                    .AddAspNetCoreInstrumentation(opts =>
                        opts.RecordException = true)
                    // EF Core query — instrument ở đây để có HTTP context
                    .AddEntityFrameworkCoreInstrumentation()
                    // ActivitySource từ Application Layer
                    .AddSource(OmsActivitySource.Name)
                )
                .WithMetrics(metrics => metrics
                    .SetResourceBuilder(resource)
                    .AddAspNetCoreInstrumentation()
                    .AddMeter(OmsMeter.Name)
                );

            var connStr = configuration["ApplicationInsights:ConnectionString"];
            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException(
                    "ApplicationInsights:ConnectionString is required in non-Development environments");
            services.AddOpenTelemetry().UseAzureMonitor(o => o.ConnectionString = connStr);


            return services;
        }
    }

}
