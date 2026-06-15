using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using OpenIddict.Validation.AspNetCore;
using OrderManagement.Application;
using OrderManagement.Application.Common.Authorization;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using OrderManagement.Infrastructure;
using OrderManagement.McpServer.Extensions;
using OrderManagement.McpServer.Middlewares;
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

var authSection = builder.Configuration.GetSection("Authentication");
var authAuthority = authSection["Authority"] ?? "http://localhost:7212/";
var authAudience = authSection["Audience"] ?? "order-management-mcp";
var authClientId = authSection["ClientId"] ?? "ordermanagement-mcp";
var authClientSecret = authSection["ClientSecret"] ?? "ordermanagement-mcp-secret";
var resourceMetadataResource = authSection["Resource"] ?? "http://localhost:5200/mcp";
var authOpenIdConfigurationUrl = new Uri(new Uri(authAuthority), ".well-known/openid-configuration");
var authAuthorizationServerMetadataUrl = new Uri(new Uri(authAuthority), ".well-known/oauth-authorization-server");
var authTokenEndpointUrl = new Uri(new Uri(authAuthority), "connect/token");
const string InspectorCorsPolicy = "InspectorCors";

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
})
    .AddMcp(options =>
    {
        options.ResourceMetadata = new ProtectedResourceMetadata
        {
            Resource = resourceMetadataResource,
            AuthorizationServers = { authAuthority },
            ScopesSupported = [ "mcp_api", authAudience, "roles", "email", "profile" ]
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Tái sử dụng cùng policy definition với Web API
    // Tập trung định nghĩa ở một chỗ: PolicyRegistrar
    PolicyRegistrar.RegisterPolicies(options);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(InspectorCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:6274")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IMcpUserContextAccessor, McpUserContextAccessor>();
builder.Services.AddScoped<McpCurrentUserService>();


builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(new Uri(authAuthority));
        options.AddAudiences(authAudience);

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

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
// Đăng ký ICurrentUserService trỏ đến McpCurrentUserService
// Application Layer chỉ biết ICurrentUserService — không biết implementation
builder.Services.AddScoped<ICurrentUserService>(
    sp => sp.GetRequiredService<McpCurrentUserService>());


var app = builder.Build();

// Middleware phải đăng ký TRƯỚC MapMcp()
app.UseMiddleware<McpErrorHandlingMiddleware>();
app.UseCors(InspectorCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<McpUserContextMiddleware>();

app.MapGet("/authorize", (HttpContext httpContext) =>
{
    var authorizeUri = new Uri(new Uri(authAuthority), "connect/authorize");
    var query = httpContext.Request.QueryString.HasValue
        ? httpContext.Request.QueryString.Value
        : string.Empty;

    return Results.Redirect(authorizeUri + query, permanent: false);
});

app.MapGet("/.well-known/openid-configuration", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    var json = await client.GetStringAsync(authOpenIdConfigurationUrl, cancellationToken);
    return Results.Text(json, "application/json");
}).RequireCors(InspectorCorsPolicy);

app.MapGet("/.well-known/oauth-authorization-server", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient();
    var json = await client.GetStringAsync(authAuthorizationServerMetadataUrl, cancellationToken);
    return Results.Text(json, "application/json");
}).RequireCors(InspectorCorsPolicy);

app.MapMethods("/token", new[] { "OPTIONS", "POST" }, async (HttpContext httpContext, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    if (HttpMethods.IsOptions(httpContext.Request.Method))
    {
        return Results.Ok();
    }

    var form = await httpContext.Request.ReadFormAsync(cancellationToken);
    var payload = form.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.ToString()));

    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, authTokenEndpointUrl)
    {
        Content = new FormUrlEncodedContent(payload)
    };

    if (httpContext.Request.Headers.TryGetValue("Authorization", out var authorization))
    {
        requestMessage.Headers.TryAddWithoutValidation("Authorization", authorization.ToString());
    }

    var client = httpClientFactory.CreateClient();
    using var response = await client.SendAsync(requestMessage, cancellationToken);
    var body = await response.Content.ReadAsStringAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

    return Results.Text(body, contentType, statusCode: (int)response.StatusCode);
}).RequireCors(InspectorCorsPolicy);

app.MapMcp("/mcp").RequireAuthorization(); // MCP server sẽ lắng nghe tại endpoint /mcp
await app.RunAsync();
