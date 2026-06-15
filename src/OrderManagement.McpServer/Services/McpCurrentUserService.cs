using Microsoft.IdentityModel.JsonWebTokens;
using OrderManagement.Application.Contracts;
using System.Security.Claims;

namespace OrderManagement.McpServer.Services
{
    /// <summary>
    /// Implementation của ICurrentUserService cho MCP context.
    /// Không đọc trực tiếp từ HttpContext.User trong MCP pipeline.
    /// User được snapshot tại middleware và lưu qua AsyncLocal accessor.
    /// </summary>
    public class McpCurrentUserService : ICurrentUserService
    {
        private static readonly string[] RoleClaimTypes = [ClaimTypes.Role, "role", "roles"];
        private readonly IMcpUserContextAccessor _userContextAccessor;

        public McpCurrentUserService(IMcpUserContextAccessor userContextAccessor)
        {
            _userContextAccessor = userContextAccessor;
        }

        private ClaimsPrincipal? User => _userContextAccessor.User;

        // Giữ lại method này để middleware/service khác có thể set snapshot nếu cần.
        public void SetUser(ClaimsPrincipal user)
        {
            _userContextAccessor.User = user;
        }

        public bool IsInRole(string role)
        {
            return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        public bool HasClaim(string claimType, string? value = null)
        {
            if (User == null)
            {
                return false;
            }

            return value is null
                ? User.HasClaim(c => c.Type == claimType)
                : User.HasClaim(claimType, value);
        }

        public Guid? UserId
        {
            get
            {
                var sub = User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                return Guid.TryParse(sub, out var userId) ? userId : null;
            }
        }

        public string? Email =>
            User?.FindFirstValue(JwtRegisteredClaimNames.Email);

        public bool IsAuthenticated =>
            User?.Identity?.IsAuthenticated ?? false;

        public IReadOnlyList<string> Roles => User?.Claims
            .Where(c => RoleClaimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        public IReadOnlyList<KeyValuePair<string, string>> GetAllClaims()
        {
            return User?.Claims
                .Select(c => new KeyValuePair<string, string>(c.Type, c.Value))
                .ToArray() ?? [];
        }
    }

}
