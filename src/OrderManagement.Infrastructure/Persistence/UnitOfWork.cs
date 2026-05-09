using Microsoft.EntityFrameworkCore.Storage;
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
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(
            ApplicationDbContext context,
            IDomainEventDispatcher dispatcher)
        {
            _context = context;
            _dispatcher = dispatcher;
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            // Không cho phép lồng transaction
            if (_currentTransaction is not null)
                throw new InvalidOperationException(
                    "Đã có transaction đang chạy. Commit hoặc Rollback trước khi bắt đầu transaction mới.");

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is null)
                throw new InvalidOperationException("Chưa có transaction nào được bắt đầu.");

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public void Dispose() => _currentTransaction?.Dispose();

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is null) return; // Idempotent — gọi nhiều lần không lỗi

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
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
