using Microsoft.Extensions.Configuration;
using OrderManagement.Application.Contracts;

namespace OrderManagement.McpServer.Services
{
    /// <summary>
    /// Fake current user cho MCP Server để chạy local tool mà không cần JWT.
    /// Cấu hình qua appsettings: FakeCurrentUser:UserId, Email, Roles.
    /// </summary>
    public sealed class FakeCurrentUserService(IConfiguration configuration) : ICurrentUserService
    {
        private readonly string[] _roles =
            configuration.GetSection("FakeCurrentUser:Roles").Get<string[]>() ?? [];

        public Guid? UserId
        {
            get
            {
                var userIdValue = configuration["FakeCurrentUser:UserId"];

                if (Guid.TryParse(userIdValue, out var id))
                {
                    return id;
                }

                // Fallback để luôn qua được case chưa đăng nhập trong command handler.
                return Guid.Parse("11111111-1111-1111-1111-111111111111");
            }
        }

        public string? Email =>
            configuration["FakeCurrentUser:Email"] ?? "mcp-user@tedu.local";

        public bool IsAuthenticated => true;

        public bool IsInRole(string role) =>
            _roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }
}
