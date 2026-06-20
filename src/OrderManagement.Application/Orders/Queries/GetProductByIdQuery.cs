using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.Orders.Queries
{
    public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<Result<ProductDetailDto?>>;

    public sealed class GetProductByIdQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto?>>
    {
        public async Task<Result<ProductDetailDto?>> Handle(
            GetProductByIdQuery request,
            CancellationToken cancellationToken)
        {
            var product = await context.Products
                .AsNoTracking()
                .Where(p => p.Id == request.ProductId)
                .Select(p => new ProductDetailDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price.Amount,
                    Currency = p.Price.Currency,
                    WeightKg = p.WeightKg,
                    StockQuantity = p.StockQuantity,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            return Result<ProductDetailDto?>.Success(product);
        }
    }
}
