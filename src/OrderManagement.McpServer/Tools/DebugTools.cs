using MediatR;
using ModelContextProtocol.Server;
using OrderManagement.Application.AuditLogs.DTOs;
using OrderManagement.Application.AuditLogs.Queries;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using System.ComponentModel;

namespace OrderManagement.McpServer.Tools
{
    [McpServerToolType]
    public class DebugTools
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IMcpUserContextAccessor _userContextAccessor;

        public DebugTools(
            IMediator mediator,
            ICurrentUserService currentUser,
            IMcpUserContextAccessor userContextAccessor)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _userContextAccessor = userContextAccessor;
        }

        [McpServerTool]
        [Description("Debug only: trả về thông tin user hiện tại. Xóa trước production.")]
        public object whoami()
        {
            var claims = _userContextAccessor.User?.Claims
                .Select(c => new { c.Type, c.Value })
                .ToArray() ?? [];

            return new
            {
                IsAuthenticated = _currentUser.IsAuthenticated,
                UserId = _currentUser.UserId,
                UserName = _currentUser.Email,
                Email = _currentUser.Email,
                Roles = _currentUser.Roles,
                Claims = claims
            };
        }

        [McpServerTool, Description("Truy vấn lịch sử hành động trong hệ thống")]
        public async Task<IReadOnlyList<AuditLogDto>?> GetAuditLogs(
            [Description("Từ ngày (ISO 8601, vd: 2025-07-01T00:00:00Z)")] DateTime? from,
            [Description("Đến ngày")] DateTime? to,
            [Description("Lọc theo actor: 'human' hoặc 'ai-agent'")] string? actorType,
            [Description("Lọc theo tên tool, vd: cancel_order")] string? toolName,
            [Description("Chỉ hiện action thất bại")] bool? failedOnly)
        {
            var result = await _mediator.Send(new GetAuditLogsQuery(
                from,
                to,
                actorType,
                toolName,
                failedOnly ?? false));

            if (result.IsFailure)
            {
                return null;
            }

            return result.Value;
        }

    }

}
