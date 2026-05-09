using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Specifications;

namespace OrderManagement.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(User user, CancellationToken ct = default);
        void Update(User user);
        void Delete(User user);

        Task<IReadOnlyList<User>> ListAsync(
            ISpecification<User> spec,
            CancellationToken ct = default);

        Task<User?> FirstOrDefaultAsync(
            ISpecification<User> spec,
            CancellationToken ct = default);

        Task<int> CountAsync(
            ISpecification<User> spec,
            CancellationToken ct = default);

        Task<bool> AnyAsync(
            ISpecification<User> spec,
            CancellationToken ct = default);
    }
}
