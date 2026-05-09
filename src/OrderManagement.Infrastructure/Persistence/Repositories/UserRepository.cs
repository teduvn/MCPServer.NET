using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.Specifications;

namespace OrderManagement.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, ct);
        }

        public async Task AddAsync(User user, CancellationToken ct = default)
        {
            await _context.Users.AddAsync(user, ct);
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
        }

        public void Delete(User user)
        {
            _context.Users.Remove(user);
        }

        public async Task<IReadOnlyList<User>> ListAsync(
            ISpecification<User> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.ToListAsync(ct);
        }

        public async Task<User?> FirstOrDefaultAsync(
            ISpecification<User> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.FirstOrDefaultAsync(ct);
        }

        public async Task<int> CountAsync(
            ISpecification<User> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.CountAsync(ct);
        }

        public async Task<bool> AnyAsync(
            ISpecification<User> spec,
            CancellationToken ct = default)
        {
            var query = ApplySpecification(spec);
            return await query.AnyAsync(ct);
        }

        private IQueryable<User> ApplySpecification(ISpecification<User> spec)
        {
            var query = _context.Users.AsQueryable();

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
