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

        public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
    }

}
