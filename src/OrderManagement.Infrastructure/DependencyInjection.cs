using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Repositories;
using OrderManagement.Infrastructure.Auth;
using OrderManagement.Infrastructure.Caching;
using OrderManagement.Infrastructure.Email;
using OrderManagement.Infrastructure.FileStorage;
using OrderManagement.Infrastructure.Identity;
using OrderManagement.Infrastructure.Payment;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories;
using OrderManagement.Infrastructure.Persistence.Seeding;
using OrderManagement.Infrastructure.Resilience;
using OrderManagement.Infrastructure.Services;
using Polly;
using SendGrid;
using StackExchange.Redis;
using Stripe;
using IdentityService = OrderManagement.Infrastructure.Services.IdentityService;

namespace OrderManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration, IHostEnvironment env)
        {
            services
               .AddPersistence(configuration, env)
               .AddRepositories()
               .AddAspNetCoreIdentity(configuration)
               .AddExternalServices(configuration)
               .AddCaching(configuration);

                return services;
        }

        // ── Persistence ──────────────────────────────────────────────────
        private static IServiceCollection AddPersistence(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment env)
        {
            // Đăng ký DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(
                            typeof(ApplicationDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(   // built-in retry cho transient error
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    });

                // Enable sensitive data logging in development
                if (env.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });
            // Map interface IUnitOfWork sang ApplicationDbContext
            // Scoped để share instance trong cùng 1 request
            services.AddScoped<IUnitOfWork>(
                sp => sp.GetRequiredService<ApplicationDbContext>());

            // Map IApplicationDbContext to ApplicationDbContext
            services.AddScoped<IApplicationDbContext>(
                sp => sp.GetRequiredService<ApplicationDbContext>());

            // Register IDbConnectionFactory
            services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

            // Đăng ký seeders
            services.AddScoped<IDataSeeder, RoleSeedDataSeeder>();
            services.AddScoped<IDataSeeder, AdminUserSeeder>();

            // Development seeder chỉ đăng ký khi chạy dev environment
            if (env.IsDevelopment())
            {
                services.AddScoped<IDataSeeder, DevelopmentCustomerSeeder>();
                services.AddScoped<IDataSeeder, DevelopmentOrderSeeder>();
            }



            return services;
        }

        // ── Repositories ─────────────────────────────────────────────────
        private static IServiceCollection AddRepositories(
            this IServiceCollection services)
        {
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            return services;
        }

        // ── External Services ─────────────────────────────────────────────
        private static IServiceCollection AddExternalServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Cấu hình từ appsettings.json
            services.Configure<SendGridSettings>(configuration.GetSection(SendGridSettings.SectionName));
            services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));

            // Register SendGrid client
            services.AddTransient<ISendGridClient>(sp =>
            {
                var apiKey = configuration["SendGrid:ApiKey"];
                return new SendGridClient(apiKey);
            });

            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
            services.AddTransient<PaymentIntentService>();
            services.AddTransient<RefundService>();

            // Đăng ký service implementations
            services.AddTransient<IEmailService, SendGridEmailService>();
            services.AddTransient<IPaymentGateway, StripePaymentGateway>();
            services.AddTransient<IFileStorage, AzureBlobFileStorage>();
            services.AddTransient<IInventoryService, InventoryService>();

            // HttpClient với Polly policy cho external HTTP calls
            // Policy = Retry bên trong CircuitBreaker (thứ tự quan trọng)
            var retryPolicy = ResiliencePolicies.GetRetryPolicy();
            var circuitBreakerPolicy = ResiliencePolicies.GetCircuitBreakerPolicy();
            var policyWrap = Policy.WrapAsync(circuitBreakerPolicy, retryPolicy);

            services.AddHttpClient("ExternalServices")
                .AddPolicyHandler(policyWrap);



            return services;
        }

        // ── Caching ───────────────────────────────────────────────────────
        private static IServiceCollection AddCaching(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Cache ──
            var cacheSettings = configuration
                .GetSection(CacheSettings.SectionName)
                .Get<CacheSettings>()!;

            services.AddSingleton(cacheSettings);

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(cacheSettings.ConnectionString));

            // Đăng ký ICacheService — interface ở Application, impl ở Infrastructure
            // Thêm vào DependencyInjection.cs
            if (configuration.GetValue<bool>("Cache:UseInMemory"))
            {
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, InMemoryCacheService>();
            }
            else
            {
                services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect(cacheSettings.ConnectionString));
                services.AddSingleton<ICacheService, RedisCacheService>();
            }


            return services;
        }

        private static IServiceCollection AddAspNetCoreIdentity(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ASP.NET Core Identity
            services
                .AddIdentityCore<AppUser>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddRoles<AppRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
                // Note: AddDefaultTokenProviders() thường dùng cho password reset, email confirmation
                // Nếu cần 2FA hoặc password reset, có thể thêm các token providers riêng
                // hoặc sử dụng AddIdentity() thay vì AddIdentityCore()

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            return services;
        }
    }

}
