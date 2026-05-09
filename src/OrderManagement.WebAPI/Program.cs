using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain;
using OrderManagement.Infrastructure;
using OrderManagement.WebAPI.Extensions;
using OrderManagement.WebAPI.Middleware;
using OrderManagement.WebAPI.Services;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger: dùng khi app đang khởi động,
// trước khi đọc được appsettings.json
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting OrderManagement API...");
    var builder = WebApplication.CreateBuilder(args);

    // Thay thế ILogger mặc định của .NET bằng Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration) // đọc từ appsettings
            .ReadFrom.Services(services)                    // cho phép inject service
            .Enrich.FromLogContext()                        // lấy property từ LogContext
            .Enrich.WithThreadId()                         // thêm ThreadId vào log
            .Enrich.WithMachineName();                     // thêm tên server
    });

    // ── Domain + Application + Infrastructure ────────────────────────────
    builder.Services
        .AddDomainServices()                              // Domain Service
        .AddApplicationServices()   // MediatR, Validation, Mapping
        .AddInfrastructureServices(builder.Configuration, builder.Environment); // DB, Cache, External

    // ── Presentation-specific ────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Configure JSON options if needed
        });

    // Configure Problem Details
    builder.Services.AddProblemDetails();

    // Configure API behavior options to properly format Problem Details
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    // ICurrentUserService — Presentation-specific implementation
    // Interface định nghĩa ở Application, implementation ở WebApi
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICorrelationIdService, CorrelationIdService>();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Host.UseDefaultServiceProvider((context, options) =>
    {
        // Validate ngay lúc build, không đợi đến runtime
        options.ValidateScopes =
            context.HostingEnvironment.IsDevelopment();
        options.ValidateOnBuild =
            context.HostingEnvironment.IsDevelopment();
    });

    // Authentication & Authorization
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization(builder.Configuration);

    builder.Services
    .AddApiVersioning(options =>
    {
        // Version mặc định khi client không chỉ định
        options.DefaultApiVersion = new ApiVersion(1, 0);

        // Tự động dùng DefaultApiVersion thay vì throw error
        options.AssumeDefaultVersionWhenUnspecified = true;

        // Trả về Sunset và api-supported-versions trong response header
        options.ReportApiVersions = true;

        // Cấu hình cách đọc version từ request
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),    // /api/v1/
            new HeaderApiVersionReader("X-Api-Version"), // header fallback
            new QueryStringApiVersionReader("api-version") // query string fallback
        );
    })
    .AddApiExplorer(options =>
    {
        // Format group name: "v1", "v2"
        options.GroupNameFormat = "'v'VVV";

        // Tự động thêm version vào URL template
        options.SubstituteApiVersionInUrl = true;
    });


    // ── Build & Middleware Pipeline ───────────────────────────────────────
    var app = builder.Build();

    // Log HTTP request/response tự động
    app.UseMiddleware<CorrelationIdMiddleware>(); // PHẢI trước UseSerilogRequestLogging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} => {StatusCode} ({Elapsed:0.0000} ms)";
    });


    // CRITICAL: ExceptionMiddleware PHẢI là middleware đầu tiên trong pipeline
    // Nếu đặt sau, các middleware trước nó có thể throw exception mà không bị catch
    app.UseGlobalExceptionHandling();


    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        // Gọi migration và seeding trước khi app lắng nghe request
        await app.InitialiseDatabaseAsync();

        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Order Management API")
                .WithTheme(ScalarTheme.Purple)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                .AddPreferredSecuritySchemes("Bearer");
        });

    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
    Log.Information("Application started. Listening on: {Urls}", urls);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API starting failed");
}
finally
{
    Log.CloseAndFlush(); // Important: flush buffer before shutting down
}





