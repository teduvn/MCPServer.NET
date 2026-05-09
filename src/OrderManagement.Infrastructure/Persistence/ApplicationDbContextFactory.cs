using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence
{
    /// <summary>
    /// Factory để tạo ApplicationDbContext cho EF Core design-time operations (migrations, etc.)
    /// </summary>
    public class ApplicationDbContextFactory
     : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Đọc connection string từ appsettings.Development.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));

            // Design-time không cần publish events, dùng NullPublisher
            return new ApplicationDbContext(optionsBuilder.Options, new NullPublisher());
        }
    }

    /// <summary>
    /// Null implementation of IPublisher for design-time operations
    /// </summary>
    internal class NullPublisher : IPublisher
    {
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Task.CompletedTask;
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

}
