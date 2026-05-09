using Dapper;
using MediatR;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Orders.Queries
{
    public record GetOrdersQuery(int Page = 1, int PageSize = 20)
        : IRequest<Result<List<OrderSummaryDto>>>;

    public sealed class GetOrdersQueryHandler
    : IRequestHandler<GetOrdersQuery, Result<List<OrderSummaryDto>>>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public GetOrdersQueryHandler(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }


        public async Task<Result<List<OrderSummaryDto>>> Handle(
            GetOrdersQuery request,
            CancellationToken cancellationToken)
        {
            const string sql = """
                    SELECT
                        o.Id,
                        o.OrderNumber,
                        o.OrderDate,
                        o.CustomerName,
                        o.TotalAmount_Amount AS TotalAmount,
                        o.TotalAmount_Currency AS Currency,
                        o.Status,
                        COUNT(oi.Id) AS ItemCount
                    FROM Orders o
                    LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
                    GROUP BY o.Id, o.OrderNumber, o.OrderDate,
                             o.CustomerName, o.TotalAmount_Amount,
                             o.TotalAmount_Currency, o.Status
                    ORDER BY o.OrderDate DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                    """;


            using var connection = _connectionFactory.CreateConnection();


            var result = await connection.QueryAsync<OrderSummaryDto>(sql, new
            {
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });


            return Result<List<OrderSummaryDto>>.Success(result.ToList());
        }
    }



}
