using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MyMcpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithTools<ContactTool>()
    .WithHttpTransport();

var app = builder.Build();
app.MapMcp("/mcp");
await app.RunAsync();