using Serilog.Context;

namespace OrderManagement.WebAPI.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;


        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }


        public async Task InvokeAsync(HttpContext context)
        {
            // Lấy từ header nếu client gửi lên (microservice forwarding)
            // Tạo mới nếu không có
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                               ?? Guid.NewGuid().ToString("N")[..12]; // 12 ký tự là đủ


            // Gắn vào LogContext — mọi log trong request này đều có CorrelationId
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                // Trả về header cho client để họ dùng khi báo lỗi
                context.Response.Headers[CorrelationIdHeader] = correlationId;


                // Thêm vào HttpContext Items để các service trong request dùng được
                context.Items["CorrelationId"] = correlationId;


                await _next(context);
            }
        }
    }

}
