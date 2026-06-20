namespace OrderManagement.McpServer.Middlewares
{
    public class ToolTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TimeSpan _defaultTimeout;


        public ToolTimeoutMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _defaultTimeout = TimeSpan.FromSeconds(
                config.GetValue<int>("McpServer:ToolTimeoutSeconds", 15));
        }


        public async Task InvokeAsync(HttpContext context)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                context.RequestAborted);
            cts.CancelAfter(_defaultTimeout);


            // Thay RequestAborted bằng linked token
            context.RequestAborted = cts.Token;


            await _next(context);
        }
    }

}
