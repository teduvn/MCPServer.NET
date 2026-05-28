using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using OrderManagement.Infrastructure;
using OrderManagement.McpServer.Middlewares;
using OrderManagement.McpServer.Resources;
using OrderManagement.McpServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Tắt console logger mặc định
//builder.Logging.ClearProviders();

// ✅ Tái sử dụng DI từ Web API — không viết lại
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);


// ✅ Đăng ký MCP Server với stdio transport
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithResources<OrderResource>()
    .WithResources<SystemInfoResource>()
    .WithPromptsFromAssembly()    // scan tất cả [McpServerPrompt] trong assembly
                                  //.WithResourcesFromAssembly()   // scan tất cả [McpServerResource] trong assembly
    ///.WithStdioServerTransport()   // stdio: dùng console in/out
    .WithToolsFromAssembly();     // scan tất cả [McpServerTool] trong assembly

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdService, CorrelationIdService>();
builder.Services.AddScoped<ICurrentUserService, FakeCurrentUserService>();

var app = builder.Build();
// Middleware phải đăng ký TRƯỚC MapMcp()
app.UseMiddleware<McpErrorHandlingMiddleware>();


app.MapMcp("/mcp"); // MCP server sẽ lắng nghe tại endpoint /mcp
await app.RunAsync();
