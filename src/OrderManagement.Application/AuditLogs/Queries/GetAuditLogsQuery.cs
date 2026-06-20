using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.AuditLogs.DTOs;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Common;

namespace OrderManagement.Application.AuditLogs.Queries
{
    public sealed record GetAuditLogsQuery(
        DateTime? From,
        DateTime? To,
        string? ActorType,
        string? ToolName,
        bool FailedOnly = false)
        : IRequest<Result<IReadOnlyList<AuditLogDto>>>;

    public sealed class GetAuditLogsQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetAuditLogsQuery, Result<IReadOnlyList<AuditLogDto>>>
    {
        public async Task<Result<IReadOnlyList<AuditLogDto>>> Handle(
            GetAuditLogsQuery request,
            CancellationToken ct)
        {
            var query = context.AuditLogs
                .AsNoTracking()
                .AsQueryable();

            if (request.From.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= request.To.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.ActorType))
            {
                query = query.Where(x => x.ActorType == request.ActorType);
            }

            if (!string.IsNullOrWhiteSpace(request.ToolName))
            {
                query = query.Where(x => x.ToolName == request.ToolName);
            }

            if (request.FailedOnly)
            {
                query = query.Where(x => !x.IsSuccess);
            }

            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .Select(x => new AuditLogDto
                {
                    CreatedAt = x.CreatedAt,
                    UserName = x.UserName,
                    ActorType = x.ActorType,
                    CommandType = x.CommandType,
                    ToolName = x.ToolName,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    IsSuccess = x.IsSuccess,
                    DurationMs = x.DurationMs,
                    ErrorMessage = x.ErrorMessage
                })
                .ToListAsync(ct);

            return Result<IReadOnlyList<AuditLogDto>>.Success(logs);
        }
    }
}
