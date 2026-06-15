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


            if (requiresRole == null) return; // Không có attribute = cho qua


            var userRoles = _currentUser.Roles; // Từ ICurrentUserService ở bài 5.3


            bool hasPermission = requiresRole.Roles
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
    }

}
