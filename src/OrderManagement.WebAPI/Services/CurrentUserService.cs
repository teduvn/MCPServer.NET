using OrderManagement.Application.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OrderManagement.WebAPI.Services
{
    /// <summary>
    /// Implementation đọc thông tin user từ JWT claims qua IHttpContextAccessor.
    /// Nằm ở WebApi — đây là layer duy nhất được biết HttpContext.
    /// </summary>
    public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
        : ICurrentUserService
    {
        private static readonly string[] RoleClaimTypes = [ClaimTypes.Role, "role", "roles"];
        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        public Guid? UserId
        {
            get
            {
                var value = User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public string? Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email);

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public IReadOnlyList<string> Roles => User?.Claims
            .Where(c => RoleClaimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

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
    }

}
