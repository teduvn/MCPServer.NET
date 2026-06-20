using OrderManagement.Application.Common.Interfaces;

namespace OrderManagement.McpServer.Middlewares
{
    public class McpUserContextMiddleware
    {
        private readonly RequestDelegate _next;

        public McpUserContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context,
            IMcpUserContextAccessor userContextAccessor)
        {
            try
            {
                // Snapshot user một lần để downstream MCP components đọc ổn định,
                // kể cả khi chúng được resolve ở scope khác trong cùng async flow.
                userContextAccessor.User = context.User?.Identity?.IsAuthenticated == true
                    ? new System.Security.Claims.ClaimsPrincipal(context.User)
                    : null;

                await _next(context);
            }
            finally
            {
                userContextAccessor.User = null;
            }
        }
    }

}
