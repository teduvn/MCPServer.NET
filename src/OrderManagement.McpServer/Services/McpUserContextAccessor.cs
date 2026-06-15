using System.Security.Claims;

namespace OrderManagement.McpServer.Services
{
    public sealed class McpUserContextAccessor : IMcpUserContextAccessor
    {
        private static readonly AsyncLocal<ClaimsPrincipal?> CurrentUser = new();

        public ClaimsPrincipal? User
        {
            get => CurrentUser.Value;
            set => CurrentUser.Value = value;
        }
    }
}
