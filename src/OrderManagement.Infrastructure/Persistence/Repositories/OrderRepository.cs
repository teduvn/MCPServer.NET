using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.Specifications;

namespace OrderManagement.Infrastructure.Persistence.Repositories
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;


        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            // Include Items vì Order là Aggregate Root
            // Items không thể load độc lập — phải qua Order
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, ct);
        }


        public void Add(Order order)
        {
            // Chỉ track — KHÔNG gọi SaveChanges
            _context.Orders.Add(order);
        }


        public void Update(Order order)
        {
            // EF Core track thay đổi tự động nếu entity đã được load
            // Gọi Update() chỉ khi entity ở disconnected state
            _context.Orders.Update(order);
        }


        public void Delete(Order order)
        {
            _context.Orders.Remove(order);
        }

        // Specification-based queries
        public async Task<IReadOnlyList<Order>> ListAsync(
            ISpecification<Order> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.ToListAsync(ct);
        }

        public async Task<Order?> FirstOrDefaultAsync(
            ISpecification<Order> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.FirstOrDefaultAsync(ct);
        }

        public async Task<int> CountAsync(
            ISpecification<Order> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.CountAsync(ct);
        }

        public async Task<bool> AnyAsync(
            ISpecification<Order> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.AnyAsync(ct);
        }

        // Helper method để apply specification vào IQueryable
        private IQueryable<Order> ApplySpecification(ISpecification<Order> spec)
        {
            // Start với base query
            var query = _context.Orders.AsQueryable();

            // Apply criteria (WHERE clause)
            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Apply includes (eager loading)
            query = spec.Includes.Aggregate(query,
                (current, include) => current.Include(include));

            // Apply ordering
            if (spec.OrderBy != null)
            {
                query = query.OrderBy(spec.OrderBy);
            }
            else if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending);
            }

            // Apply paging
            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }

            return query;
        }
    }

}
