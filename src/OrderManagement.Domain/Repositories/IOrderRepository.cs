using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Specifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Repositories
{
    public interface IOrderRepository
    {
        // CRUD cơ bản — không thay đổi
        Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
        void Add(Order order);
        void Update(Order order);
        void Delete(Order order);

        // Specification-based query — thay thế hàng chục method cũ
        Task<IReadOnlyList<Order>> ListAsync(
            ISpecification<Order> spec,
            CancellationToken ct = default);

        Task<Order?> FirstOrDefaultAsync(
            ISpecification<Order> spec,
            CancellationToken ct = default);

        Task<int> CountAsync(
            ISpecification<Order> spec,
            CancellationToken ct = default);

        Task<bool> AnyAsync(
            ISpecification<Order> spec,
            CancellationToken ct = default);
    }

}
