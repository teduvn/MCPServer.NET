using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Commit tất cả thay đổi đang chờ vào database
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // Chạy toàn bộ logic trong 1 retriable transaction unit (tương thích EF execution strategy)
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default);
    }
}
