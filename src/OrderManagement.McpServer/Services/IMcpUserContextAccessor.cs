using System.Security.Claims;

namespace OrderManagement.McpServer.Services
{
    public interface IMcpUserContextAccessor
    {
        ClaimsPrincipal? User { get; set; }
    }
}
