using Dapper;
using MediatR;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Orders.Queries
{
    /// <summary>
    /// Query lấy danh sách Orders với pagination và filter theo status.
    /// </summary>
    public sealed record GetOrdersPagedQuery(int Page, int PageSize, OrderStatus? Status = null)
        : IRequest<Result<PagedResult<OrderSummaryDto>>>;

    /// <summary>
    /// Handler xử lý GetOrdersPagedQuery.
    /// Trả về danh sách orders với metadata pagination.
    /// </summary>
    public sealed class GetOrdersPagedQueryHandler
        : IRequestHandler<GetOrdersPagedQuery, Result<PagedResult<OrderSummaryDto>>>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public GetOrdersPagedQueryHandler(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(
            GetOrdersPagedQuery request,
            CancellationToken cancellationToken)
        {
            // Build dynamic SQL với filter status
            var whereClause = request.Status.HasValue 
                ? "WHERE o.Status = @Status" 
                : string.Empty;

            var sql = $"""
                    SELECT
                        o.Id,
                        CAST(o.Id AS NVARCHAR(36)) AS OrderNumber,
                        o.CreatedAt AS OrderDate,
                        o.CustomerEmail AS CustomerName,
                        o.TotalAmount,
                        o.Currency,
                        o.Status,
                        COUNT(oi.Id) AS ItemCount
                    FROM Orders o
                    LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
                    {whereClause}
                    GROUP BY o.Id, o.CustomerId, o.CustomerEmail,
                             o.TotalAmount, o.Currency, o.Status, o.CreatedAt
                    ORDER BY o.CreatedAt DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(DISTINCT o.Id)
                    FROM Orders o
                    {whereClause};
                    """;

            using var connection = _connectionFactory.CreateConnection();

            // Execute multi-query để lấy cả data và total count
            using var multi = await connection.QueryMultipleAsync(sql, new
            {
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize,
                Status = request.Status?.ToString()
            });

            var items = (await multi.ReadAsync<OrderSummaryDto>()).ToList();
            var totalCount = await multi.ReadSingleAsync<int>();

            var pagedResult = new PagedResult<OrderSummaryDto>(
                items,
                totalCount,
                request.Page,
                request.PageSize
            );

            return Result<PagedResult<OrderSummaryDto>>.Success(pagedResult);
        }
    }
}
