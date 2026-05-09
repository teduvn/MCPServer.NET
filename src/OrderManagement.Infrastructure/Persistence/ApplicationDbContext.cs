using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Identity;
using OrderManagement.Infrastructure.Persistence.Outbox;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OrderManagement.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IApplicationDbContext, IUnitOfWork
    {
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public new DbSet<User> Users => Set<User>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        private readonly IPublisher _publisher;


        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IPublisher publisher) : base(options)
        {
            _publisher = publisher;
        }


        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            // Trước khi save, convert domain events thành OutboxMessage
            var outboxMessages = ChangeTracker
                .Entries<Entity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .Select(domainEvent => new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = domainEvent.GetType().FullName!,
                    Content = JsonSerializer.Serialize(domainEvent,
                                    domainEvent.GetType(),
                                    new JsonSerializerOptions { WriteIndented = false }),
                    OccurredAt = DateTime.UtcNow
                })
                .ToList();


            // Clear domain events khỏi entity
            ChangeTracker.Entries<Entity>()
                .ToList()
                .ForEach(e => e.Entity.ClearDomainEvents());


            // Thêm OutboxMessage vào DbContext — cùng transaction với entity
            await OutboxMessages.AddRangeAsync(outboxMessages, ct);


            // 1. Lưu xuống DB trước
            var result = await base.SaveChangesAsync(ct);

            // 2. Sau khi commit thành công, dispatch domain events
            // (Bài này dùng cách đơn giản — Bài Outbox sẽ dùng cách robust hơn)
            await DispatchDomainEventsAsync(ct);

            return result;
        }


        private async Task DispatchDomainEventsAsync(CancellationToken ct)
        {
            // Lấy tất cả entity có domain event chưa dispatch
            var entities = ChangeTracker
                .Entries<Entity>()   // Entity base class có DomainEvents collection
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();


            var domainEvents = entities
                .SelectMany(e => e.DomainEvents)
                .ToList();


            // Clear trước khi dispatch để tránh dispatch lại nếu có lỗi
            entities.ForEach(e => e.ClearDomainEvents());


            foreach (var domainEvent in domainEvents)
                await _publisher.Publish(domainEvent, ct);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply tất cả IEntityTypeConfiguration trong assembly này
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }


        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
