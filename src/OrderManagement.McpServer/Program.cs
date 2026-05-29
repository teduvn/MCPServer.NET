using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Application;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using OrderManagement.Infrastructure;
using OrderManagement.McpServer.Extensions;
using OrderManagement.McpServer.Middlewares;
using OrderManagement.McpServer.Tools;
using OrderManagement.McpServer.Resources;
using OrderManagement.McpServer.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ User Secrets tự động được load trong Development environment
// Configuration priority (cao → thấp):
//   1. Command-line arguments
//   2. Environment variables
//   3. User Secrets (Development only)
//   4. appsettings.{Environment}.json
//   5. appsettings.json
//
// Để set/view secrets:
//   dotnet user-secrets set "ApplicationInsights:ConnectionString" "your-value"
//   dotnet user-secrets list

// Tắt console logger mặc định
//builder.Logging.ClearProviders();

// ✅ Tái sử dụng DI từ Web API — không viết lại
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddOmsObservability(builder.Configuration, builder.Environment);

// ✅ Đăng ký MCP Server với stdio transport
//builder.Services.AddScoped<OrderTools>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithResources<OrderResource>()
    .WithResources<SystemInfoResource>()
    .WithPromptsFromAssembly()    // scan tất cả [McpServerPrompt] trong assembly
                                  //.WithResourcesFromAssembly()   // scan tất cả [McpServerResource] trong assembly
    //.WithStdioServerTransport()   // stdio: dùng console in/out
    .WithToolsFromAssembly();     // scan tất cả [McpServerTool] trong assembly

// Bật ValidateScopes cho Development
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;     // Bắt captive dependency
        options.ValidateOnBuild = true;    // Validate ngay khi build container
    });
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdService, CorrelationIdService>();
builder.Services.AddScoped<ICurrentUserService, FakeCurrentUserService>();

var app = builder.Build();

// Middleware phải đăng ký TRƯỚC MapMcp()
app.UseMiddleware<McpErrorHandlingMiddleware>();

app.MapMcp("/mcp"); // MCP server sẽ lắng nghe tại endpoint /mcp
await app.RunAsync();
