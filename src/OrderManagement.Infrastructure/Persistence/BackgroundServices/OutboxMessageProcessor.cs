using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace OrderManagement.Infrastructure.Persistence.BackgroundServices
{
    public sealed class OutboxMessageProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxMessageProcessor> _logger;


        public OutboxMessageProcessor(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxMessageProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
                // Chạy mỗi 10 giây — production có thể dùng Quartz hoặc Hangfire
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }


        private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();


            // Lấy tối đa 20 message chưa xử lý
            var messages = await context.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.OccurredAt)
                .Take(20)
                .ToListAsync(ct);


            foreach (var message in messages)
            {
                try
                {
                    // Deserialize về đúng type domain event
                    var eventType = Type.GetType(message.Type)!;
                    var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(
                        message.Content, eventType)!;


                    await publisher.Publish(domainEvent, ct);


                    // Đánh dấu đã xử lý
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                    message.Error = ex.Message;
                    // Không throw — tiếp tục xử lý message khác
                    // Message này sẽ được retry ở lần chạy kế tiếp
                }
            }


            await context.SaveChangesAsync(ct);
        }
    }

}
