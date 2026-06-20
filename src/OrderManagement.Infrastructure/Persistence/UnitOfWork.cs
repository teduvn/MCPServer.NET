using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence
{

    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IDomainEventDispatcher _dispatcher;

        public UnitOfWork(
            ApplicationDbContext context,
            IDomainEventDispatcher dispatcher)
        {
            _context = context;
            _dispatcher = dispatcher;
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction is not null)
            {
                await operation(cancellationToken);
                return;
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                await operation(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            });
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction is not null)
                return await operation(cancellationToken);

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                var result = await operation(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return result;
            });
        }

        public void Dispose()
        {
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            // Lấy tất cả entities có domain event trước khi save
            var entitiesWithEvents = _context.ChangeTracker
                .Entries<Entity>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            // Save data trước
            var result = await _context.SaveChangesAsync(ct);

            // Dispatch event SAU khi save thành công
            await _dispatcher.DispatchEventsAsync(entitiesWithEvents, ct);

            return result;
        }
    }

}
