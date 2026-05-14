using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Application;
using OrderManagement.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Tắt console logger mặc định
builder.Logging.ClearProviders();

// ✅ Tái sử dụng DI từ Web API — không viết lại
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);


// ✅ Đăng ký MCP Server với stdio transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()   // stdio: dùng console in/out
    .WithToolsFromAssembly();     // scan tất cả [McpServerTool] trong assembly


var app = builder.Build();
await app.RunAsync();
