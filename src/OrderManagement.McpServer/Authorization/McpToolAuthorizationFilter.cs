using OrderManagement.Application.Contracts;
using System.Reflection;

namespace OrderManagement.McpServer.Authorization
{
    public class McpToolAuthorizationFilter
    {
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<McpToolAuthorizationFilter> _logger;


        public McpToolAuthorizationFilter(
            ICurrentUserService currentUser,
            ILogger<McpToolAuthorizationFilter> logger)
        {
            _currentUser = currentUser;
            _logger = logger;
        }


        public void CheckAuthorization(MethodInfo toolMethod)
        {
            var requiresRole = toolMethod
                .GetCustomAttributes<RequiresRoleAttribute>()
                .FirstOrDefault();
            var requiresClaim = toolMethod
                .GetCustomAttributes<RequiresClaimAttribute>()
                .FirstOrDefault();

            if (requiresRole == null && requiresClaim == null) return; // Không có attribute = cho qua


            var userRoles = _currentUser.Roles; // Từ ICurrentUserService ở bài 5.3


            if (requiresRole is not null)
            {
                var hasPermission = requiresRole.Roles
                    .Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

                if (!hasPermission)
                {
                    _logger.LogWarning(
                        "Unauthorized tool access. User {UserId} ({UserRoles}) attempted" +
                        " to call tool requiring roles: {RequiredRoles}",
                        _currentUser.UserId,
                        string.Join(",", userRoles),
                        string.Join(",", requiresRole.Roles));

                    // Throw exception với message rõ ràng để AI đọc hiểu
                    throw new UnauthorizedAccessException(
                        $"Access denied. Required role(s): {string.Join(" or ", requiresRole.Roles)}. " +
                        $"Your current role: {string.Join(",", userRoles)}.");
                }
            }

            if (requiresClaim is null)
            {
                return;
            }

            var hasClaim = requiresClaim.Values.Length == 0
                ? _currentUser.HasClaim(requiresClaim.ClaimType)
                : requiresClaim.Values.Any(value => _currentUser.HasClaim(requiresClaim.ClaimType, value));

            if (hasClaim)
            {
                return;
            }

            _logger.LogWarning(
                "Unauthorized tool access. User {UserId} missing claim {ClaimType}={ClaimValues}",
                _currentUser.UserId,
                requiresClaim.ClaimType,
                string.Join(",", requiresClaim.Values));

            throw new UnauthorizedAccessException(
                $"Access denied. Required claim: {requiresClaim.ClaimType}=" +
                $"{string.Join(" or ", requiresClaim.Values)}.");
        }
    }

}
