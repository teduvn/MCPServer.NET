using ModelContextProtocol.Server;
using OrderManagement.Application.Contracts;
using OrderManagement.McpServer.Services;
using System.ComponentModel;

namespace OrderManagement.McpServer.Tools
{
    [McpServerToolType]
    public class DebugTools
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IMcpUserContextAccessor _userContextAccessor;

        public DebugTools(
            ICurrentUserService currentUser,
            IMcpUserContextAccessor userContextAccessor)
        {
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
    }

}
