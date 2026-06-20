using System.Security.Claims;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IMcpUserContextAccessor
    {
        ClaimsPrincipal? User { get; set; }
    }
}
