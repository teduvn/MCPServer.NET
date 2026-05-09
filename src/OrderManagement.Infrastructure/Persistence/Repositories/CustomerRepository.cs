using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.Specifications;

namespace OrderManagement.Infrastructure.Persistence.Repositories
{
    public sealed class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public void Add(Customer customer)
        {
            _context.Customers.Add(customer);
        }

        public void Update(Customer customer)
        {
            _context.Customers.Update(customer);
        }

        public void Delete(Customer customer)
        {
            _context.Customers.Remove(customer);
        }

        public async Task<IReadOnlyList<Customer>> ListAsync(
            ISpecification<Customer> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.ToListAsync(ct);
        }

        public async Task<Customer?> FirstOrDefaultAsync(
            ISpecification<Customer> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.FirstOrDefaultAsync(ct);
        }

        public async Task<int> CountAsync(
            ISpecification<Customer> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.CountAsync(ct);
        }

        public async Task<bool> AnyAsync(
            ISpecification<Customer> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.AnyAsync(ct);
        }

        private IQueryable<Customer> ApplySpecification(ISpecification<Customer> spec)
        {
            var query = _context.Customers.AsQueryable();

            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            query = spec.Includes.Aggregate(query,
                (current, include) => current.Include(include));

            if (spec.OrderBy != null)
            {
                query = query.OrderBy(spec.OrderBy);
            }
            else if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending);
            }

            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }

            return query;
        }
    }
}
