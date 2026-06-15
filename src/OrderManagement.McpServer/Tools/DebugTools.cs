using ModelContextProtocol.Server;
using OrderManagement.Application.Contracts;
using System.ComponentModel;

namespace OrderManagement.McpServer.Tools
{
    [McpServerToolType]
    public class DebugTools
    {
        private readonly ICurrentUserService _currentUser;

        public DebugTools(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }

        [McpServerTool]
        [Description("Debug only: trả về thông tin user hiện tại. Xóa trước production.")]
        public object whoami()
        {
            return new
            {
                IsAuthenticated = _currentUser.IsAuthenticated,
                UserId = _currentUser.UserId,
                UserName = _currentUser.Email,
                Email = _currentUser.Email,
            };
        }
    }

}
