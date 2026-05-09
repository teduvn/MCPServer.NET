using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Specifications;

namespace OrderManagement.Domain.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
        void Add(Customer customer);
        void Update(Customer customer);
        void Delete(Customer customer);

        Task<IReadOnlyList<Customer>> ListAsync(
            ISpecification<Customer> spec,
            CancellationToken ct = default);

        Task<Customer?> FirstOrDefaultAsync(
            ISpecification<Customer> spec,
            CancellationToken ct = default);

        Task<int> CountAsync(
            ISpecification<Customer> spec,
            CancellationToken ct = default);

        Task<bool> AnyAsync(
            ISpecification<Customer> spec,
            CancellationToken ct = default);
    }
}
