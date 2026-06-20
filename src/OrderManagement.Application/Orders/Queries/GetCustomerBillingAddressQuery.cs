using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Orders.Queries
{
    public sealed record GetCustomerBillingAddressQuery(Guid CustomerId) : IRequest<Result<AddressDto?>>;

    public sealed class GetCustomerBillingAddressQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetCustomerBillingAddressQuery, Result<AddressDto?>>
    {
        public async Task<Result<AddressDto?>> Handle(
            GetCustomerBillingAddressQuery request,
            CancellationToken cancellationToken)
        {
            var address = await context.Customers
                .AsNoTracking()
                .Where(c => c.Id == request.CustomerId)
                .Select(c => c.BillingAddress == null
                    ? null
                    : new AddressDto
                    {
                        Street = c.BillingAddress.Street,
                        City = c.BillingAddress.City,
                        Province = c.BillingAddress.Province,
                        PostalCode = c.BillingAddress.PostalCode,
                        Country = c.BillingAddress.Country,
                        FormattedAddress = c.BillingAddress.ToString()
                    })
                .FirstOrDefaultAsync(cancellationToken);

            return Result<AddressDto?>.Success(address);
        }
    }
}
