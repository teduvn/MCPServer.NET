using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.McpServer.Middlewares
{
    public class McpErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<McpErrorHandlingMiddleware> _logger;


        public McpErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<McpErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }


        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log đầy đủ để dev debug
                _logger.LogError(ex,
                    "Unhandled exception in MCP Server. Path: {Path}",
                    context.Request.Path);


                // Trả về MCP-compatible error response
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";


                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = -32603,  // JSON-RPC Internal Error
                        message = "Internal server error. Please try again later."
                        // KHÔNG expose ex.Message — có thể chứa thông tin nhạy cảm
                    }
                });
            }
        }
    }

}
