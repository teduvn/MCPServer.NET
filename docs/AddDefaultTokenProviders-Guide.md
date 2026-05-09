# Hướng dẫn thêm AddDefaultTokenProviders

## Context
ASP.NET Core Identity trong .NET 10 có 2 cách setup:

### 1. AddIdentityCore (Minimal - Hiện tại)
```csharp
services.AddIdentityCore<AppUser>()
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```
**Phù hợp cho:** API authentication, không cần 2FA/Password Reset

### 2. AddIdentity (Full features)
```csharp
services.AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
```
**Cần thêm:** SignInManager, Cookie Authentication (không phù hợp cho JWT-only API)

## Nếu cần AddDefaultTokenProviders trong .NET 10

### Bước 1: Kiểm tra package
File `OrderManagement.Infrastructure.csproj` cần có:
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.6" />
```
✅ Đã có rồi

### Bước 2: Sử dụng AddIdentity thay vì AddIdentityCore
```csharp
private static IServiceCollection AddAspNetCoreIdentity(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services
        .AddIdentity<AppUser, AppRole>(options =>
        {
            // Password options
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;

            // User options
            options.User.RequireUniqueEmail = true;

            // Lockout options
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            // Token options
            options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
            options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders(); // ✅ Có sẵn với AddIdentity

    services.AddScoped<IIdentityService, IdentityService>();
    return services;
}
```

### Lưu ý khi dùng AddIdentity
- AddIdentity tự động thêm SignInManager và Cookie authentication
- Nếu API chỉ dùng JWT, cần configure để disable cookies:

```csharp
services.AddIdentity<AppUser, AppRole>(options => { /* ... */ })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Disable automatic cookie authentication for JWT APIs
services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});
```

## Token Providers được thêm bởi AddDefaultTokenProviders()

1. **DataProtectorTokenProvider** - Password reset tokens
2. **PhoneNumberTokenProvider** - Phone confirmation
3. **EmailTokenProvider** - Email confirmation
4. **AuthenticatorTokenProvider** - 2FA authenticator app

## Kết luận

**Hiện tại:** Code của bạn dùng `AddIdentityCore` - đủ cho JWT authentication cơ bản ✅

**Nếu cần:** Password reset, Email confirmation, 2FA → Chuyển sang `AddIdentity` + `AddDefaultTokenProviders()`
