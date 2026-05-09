using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Common.Exceptions;

namespace OrderManagement.WebAPI.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);  // Cho request đi qua pipeline bình thường
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log TRƯỚC khi quyết định response — luôn log đầy đủ server-side
            _logger.LogError(exception,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var (statusCode, problemDetails) = MapException(exception, context);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(problemDetails);
        }

        private (int statusCode, ProblemDetails details) MapException(
            Exception exception,
            HttpContext context)
        {
            var instance = context.Request.Path;

            return exception switch
            {
                ValidationException ve => (422, new ValidationProblemDetails(
                    ve.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.ErrorMessage).ToArray()))
                {
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Title = "Validation Failed",
                    Status = 422,
                    Instance = instance
                }),

                NotFoundException nfe => (404, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Resource Not Found",
                    Status = 404,
                    Detail = nfe.Message,
                    Instance = instance
                }),

                DomainException de => (400, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Business Rule Violation",
                    Status = 400,
                    Detail = de.Message,
                    Instance = instance
                }),

                ConflictException ce => (409, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title = "Conflict",
                    Status = 409,
                    Detail = ce.Message,
                    Instance = instance
                }),

                UnauthorizedException ue => (401, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = ue.Message,
                    Instance = instance
                }),

                ForbiddenException => (403, new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title = "Forbidden",
                    Status = 403,
                    Detail = "Bạn không có quyền thực hiện thao tác này.",
                    Instance = instance
                }),

                // Catch-all: ẩn detail, chỉ expose traceId để debug
                _ => (500, BuildInternalServerError(context))
            };
        }

        private ProblemDetails BuildInternalServerError(HttpContext context)
        {
            var pd = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "Đã có lỗi xảy ra. Vui lòng thử lại sau.",
                Instance = context.Request.Path
            };

            // Chỉ expose traceId để team dev có thể correlate log
            // KHÔNG expose stack trace, exception message, hay bất kỳ internal detail
            pd.Extensions["traceId"] = context.TraceIdentifier;

            // Development environment: cho phép thấy thêm thông tin để debug
            // Production: ẩn hoàn toàn
            if (_env.IsDevelopment())
            {
                // pd.Extensions["debug"] = ...  // thêm vào nếu cần
            }

            return pd;
        }
    }

}
