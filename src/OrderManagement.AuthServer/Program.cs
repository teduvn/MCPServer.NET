using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OrderManagement.AuthServer.Data;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

var seedOptions = GetSeedOptions(builder.Configuration);
var seededUsers = GetSeedUsers(seedOptions);
var mcpAudience = seedOptions.Audience ?? "order-management-mcp";
const string InspectorCorsPolicy = "InspectorCors";

builder.Services.AddRazorPages();
builder.Services.AddCors(options =>
{
    options.AddPolicy(InspectorCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:6274")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseInMemoryDatabase("OrderManagementAuth");
    options.UseOpenIddict();
});

builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
    .AddCookie(IdentityConstants.ApplicationScheme);
builder.Services.AddAuthorization();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AuthDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetIntrospectionEndpointUris("/connect/introspect");

        options.AllowPasswordFlow();
        options.AllowAuthorizationCodeFlow()
            .RequireProofKeyForCodeExchange();
        options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles, "mcp_api");
        options.DisableAccessTokenEncryption();

        ConfigureOpenIddictCertificates(options, builder.Configuration, builder.Environment);

        var aspNetCoreBuilder = options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough();

        if (builder.Environment.IsDevelopment())
        {
            aspNetCoreBuilder.DisableTransportSecurityRequirement();
        }
    });

var app = builder.Build();

await SeedAsync(app.Services, builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseCors(InspectorCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/login", (HttpContext httpContext) =>
{
    var returnUrl = httpContext.Request.Query["returnUrl"].ToString();
    return BuildLoginPage(seededUsers, returnUrl);
});

app.MapPost("/login", async (
    HttpContext httpContext,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var user = await userManager.FindByNameAsync(username)
        ?? await userManager.FindByEmailAsync(username);

    if (user is null)
    {
        return BuildLoginPage(
            seededUsers,
            returnUrl,
            errorMessage: "Tên đăng nhập hoặc mật khẩu không đúng.",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var passwordValid = await signInManager.CheckPasswordSignInAsync(
        user,
        password,
        lockoutOnFailure: false);

    if (!passwordValid.Succeeded)
    {
        return BuildLoginPage(
            seededUsers,
            returnUrl,
            errorMessage: "Tên đăng nhập hoặc mật khẩu không đúng.",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    await signInManager.SignOutAsync();
    await signInManager.SignInAsync(user, isPersistent: false);

    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
});

app.MapPost("/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});

app.MapGet("/connect/authorize", async (
    HttpContext httpContext,
    UserManager<IdentityUser> userManager) =>
{
    var request = httpContext.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("OpenIddict request is not available.");

    var promptValues = request.Prompt?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
    if (promptValues.Contains("login", StringComparer.OrdinalIgnoreCase))
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    if (httpContext.User.Identity?.IsAuthenticated != true)
    {
        var returnUrl = $"{httpContext.Request.PathBase}{httpContext.Request.Path}{httpContext.Request.QueryString}";
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var user = await userManager.GetUserAsync(httpContext.User);
    if (user is null)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var returnUrl = $"{httpContext.Request.PathBase}{httpContext.Request.Path}{httpContext.Request.QueryString}";
        return Results.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var principal = await CreatePrincipalAsync(userManager, user, request.GetScopes(), mcpAudience);

    return Results.SignIn(
        principal,
        authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.MapPost("/connect/token", async (
    HttpContext httpContext,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager) =>
{
    var request = httpContext.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("OpenIddict request is not available.");

    if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
    {
        var result = await httpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (result.Principal is null)
        {
            return Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Authorization code is invalid or expired."
                }),
                [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var subject = result.Principal.GetClaim(Claims.Subject);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Authorization code or refresh token subject is missing."
                }),
                [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var tokenUser = await userManager.FindByIdAsync(subject);
        if (tokenUser is null)
        {
            return BuildInvalidGrantResult();
        }

        var tokenPrincipal = await CreatePrincipalAsync(
            userManager,
            tokenUser,
            result.Principal.GetScopes(),
            mcpAudience);

        return Results.SignIn(
            tokenPrincipal,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    if (!request.IsPasswordGrantType())
    {
        return Results.BadRequest(new
        {
            error = Errors.UnsupportedGrantType,
            error_description = "AuthServer local chỉ bật password grant và authorization code + PKCE."
        });
    }

    var user = await userManager.FindByNameAsync(request.Username!)
        ?? await userManager.FindByEmailAsync(request.Username!);

    if (user is null)
    {
        return BuildInvalidGrantResult();
    }

    var passwordValid = await signInManager.CheckPasswordSignInAsync(
        user,
        request.Password!,
        lockoutOnFailure: false);

    if (!passwordValid.Succeeded)
    {
        return BuildInvalidGrantResult();
    }

    var principal = await CreatePrincipalAsync(userManager, user, request.GetScopes(), mcpAudience);

    return Results.SignIn(
        principal,
        authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.MapGet("/", () => Results.Json(new
{
    message = "OrderManagement AuthServer is running.",
    authorizationEndpoint = "/connect/authorize",
    tokenEndpoint = "/connect/token",
    introspectionEndpoint = "/connect/introspect",
    loginEndpoint = "/login",
    seededUsers = seededUsers.Select(user => new
    {
        user.UserName,
        user.Email,
        user.Password,
        user.Roles
    }),
    audience = mcpAudience
}));

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

IResult BuildInvalidGrantResult()
{
    return Results.Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Tên đăng nhập hoặc mật khẩu không đúng."
        }),
        [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}

static async Task<ClaimsPrincipal> CreatePrincipalAsync(
    UserManager<IdentityUser> userManager,
    IdentityUser user,
    IEnumerable<string> scopes,
    string audience)
{
    var identity = new ClaimsIdentity(
        authenticationType: TokenValidationParameters.DefaultAuthenticationType,
        nameType: Claims.Name,
        roleType: Claims.Role);

    identity.AddClaim(Claims.Subject, user.Id);
    identity.AddClaim(Claims.Name, user.UserName ?? user.Email ?? user.Id);

    if (!string.IsNullOrWhiteSpace(user.Email))
    {
        identity.AddClaim(Claims.Email, user.Email);
    }

    var roles = await userManager.GetRolesAsync(user);
    foreach (var role in roles)
    {
        identity.AddClaim(Claims.Role, role);
    }

    foreach (var permission in GetPermissionsForRoles(roles))
    {
        identity.AddClaim("permission", permission);
    }

    var principal = new ClaimsPrincipal(identity);
    var grantedScopes = scopes
        .Append("mcp_api")
        .Append(audience)
        .Append(Scopes.Roles)
        .Append(Scopes.Email)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    principal.SetScopes(grantedScopes);
    principal.SetResources(audience);
    principal.SetDestinations(static claim => claim.Type switch
    {
        Claims.Subject => [Destinations.AccessToken],
        Claims.Name => [Destinations.AccessToken],
        Claims.Email => [Destinations.AccessToken],
        Claims.Role => [Destinations.AccessToken],
        "permission" => [Destinations.AccessToken],
        _ => [Destinations.AccessToken]
    });

    return principal;
}

static IReadOnlyCollection<string> GetPermissionsForRoles(IEnumerable<string> roles)
{
    var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var role in roles)
    {
        switch (role)
        {
            case "Customer":
                permissions.Add(AuthPermissions.Orders.View);
                break;
            case "Analyst":
                permissions.Add(AuthPermissions.Reports.ViewRevenue);
                break;
            case "Manager":
                permissions.Add(AuthPermissions.Orders.View);
                permissions.Add(AuthPermissions.Orders.Cancel);
                permissions.Add(AuthPermissions.Orders.Manage);
                permissions.Add(AuthPermissions.Reports.ViewRevenue);
                break;
            case "Admin":
                permissions.Add(AuthPermissions.Orders.View);
                permissions.Add(AuthPermissions.Orders.Cancel);
                permissions.Add(AuthPermissions.Orders.Manage);
                permissions.Add(AuthPermissions.Reports.ViewRevenue);
                break;
        }
    }

    return permissions.ToArray();
}

static IResult BuildLoginPage(
    IReadOnlyCollection<SeedUserDescriptor> seededUsers,
    string? returnUrl,
    string? errorMessage = null,
    int statusCode = StatusCodes.Status200OK)
{
    var encodedReturnUrl = WebUtility.HtmlEncode(returnUrl ?? string.Empty);
    var encodedError = string.IsNullOrWhiteSpace(errorMessage)
        ? string.Empty
        : $"<p style=\"color:#b91c1c;font-weight:600;\">{WebUtility.HtmlEncode(errorMessage)}</p>";

    var accountsMarkup = string.Join(
        Environment.NewLine,
        seededUsers.Select(user =>
            $"<tr><td>{WebUtility.HtmlEncode(user.UserName)}</td><td>{WebUtility.HtmlEncode(user.Password)}</td><td>{WebUtility.HtmlEncode(string.Join(", ", user.Roles))}</td></tr>"));

    var html = $$"""
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>OrderManagement Auth Login</title>
    <style>
        body { font-family: Segoe UI, sans-serif; background: #f5f7fb; color: #1f2937; padding: 32px; }
        .container { max-width: 760px; margin: 0 auto; background: #fff; border-radius: 16px; padding: 32px; box-shadow: 0 12px 30px rgba(15, 23, 42, 0.08); }
        h1 { margin-top: 0; }
        form { display: grid; gap: 12px; margin-bottom: 24px; }
        label { font-weight: 600; display: grid; gap: 6px; }
        input { padding: 10px 12px; border: 1px solid #cbd5e1; border-radius: 10px; font-size: 14px; }
        button { width: fit-content; padding: 10px 16px; border: 0; border-radius: 10px; background: #0f766e; color: white; font-weight: 600; cursor: pointer; }
        table { width: 100%; border-collapse: collapse; }
        th, td { text-align: left; padding: 10px; border-bottom: 1px solid #e5e7eb; }
        code { background: #f1f5f9; padding: 2px 6px; border-radius: 6px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>Dang nhap AuthServer</h1>
        <p>Authorization code flow se dung cookie login nay de cap token cho dung tai khoan ban chon.</p>
        {{encodedError}}
        <form method="post" action="/login">
            <input type="hidden" name="returnUrl" value="{{encodedReturnUrl}}" />
            <label>
                Username hoac email
                <input type="text" name="username" autocomplete="username" required />
            </label>
            <label>
                Password
                <input type="password" name="password" autocomplete="current-password" required />
            </label>
            <button type="submit">Dang nhap</button>
        </form>

        <h2>Tai khoan seed de test</h2>
        <table>
            <thead>
                <tr>
                    <th>Username</th>
                    <th>Password</th>
                    <th>Roles</th>
                </tr>
            </thead>
            <tbody>
                {{accountsMarkup}}
            </tbody>
        </table>
        <p>Nếu muốn đổi user khi authorize lại, gọi <code>prompt=login</code> hoặc logout rồi đăng nhập user khác.</p>
    </div>
</body>
</html>
""";

    return Results.Content(html, "text/html; charset=utf-8", Encoding.UTF8, statusCode);
}

static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
{
    using var scope = services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    await dbContext.Database.EnsureCreatedAsync();

    var seedOptions = GetSeedOptions(configuration);
    var seededUsers = GetSeedUsers(seedOptions);
    var audience = seedOptions.Audience ?? "order-management-mcp";

    var roleNames = seededUsers
        .SelectMany(user => user.Roles)
        .Concat(["Admin", "Analyst", "Customer", "Manager"])
        .Where(role => !string.IsNullOrWhiteSpace(role))
        .Distinct(StringComparer.OrdinalIgnoreCase);

    foreach (var roleName in roleNames)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            continue;
        }

        var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed role failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }
    }

    foreach (var seededUser in seededUsers)
    {
        var user = await userManager.FindByNameAsync(seededUser.UserName)
            ?? await userManager.FindByEmailAsync(seededUser.Email);

        if (user is null)
        {
            user = new IdentityUser
            {
                Id = seededUser.Id,
                UserName = seededUser.UserName,
                Email = seededUser.Email,
                EmailConfirmed = true
            };

            var createUserResult = await userManager.CreateAsync(user, seededUser.Password);
            if (!createUserResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Seed user failed: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
            }
        }

        foreach (var roleName in seededUser.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await userManager.IsInRoleAsync(user, roleName))
            {
                continue;
            }

            var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
            if (!addToRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Seed user role failed: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    var clientId = seedOptions.ClientId ?? "ordermanagement-mcp";
    var clientSecret = seedOptions.ClientSecret ?? "ordermanagement-mcp-secret";
    var inspectorRedirectUri = seedOptions.InspectorRedirectUri ?? "http://localhost:6274/oauth/callback";

    if (await applicationManager.FindByClientIdAsync(clientId) is null)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "OrderManagement MCP client"
        };

        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + Scopes.Email);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + Scopes.Profile);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + Scopes.Roles);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "mcp_api");
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + audience);
        descriptor.RedirectUris.Add(new Uri(inspectorRedirectUri));
        descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

        await applicationManager.CreateAsync(descriptor);
    }
}

static void ConfigureOpenIddictCertificates(
    Microsoft.Extensions.DependencyInjection.OpenIddictServerBuilder options,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    if (environment.IsDevelopment())
    {
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();
        return;
    }

    var certificatePath = configuration["OpenIddict:Certificates:Path"];
    var certificatePassword = configuration["OpenIddict:Certificates:Password"];

    if (!string.IsNullOrWhiteSpace(certificatePath))
    {
        var resolvedPath = Path.IsPathRooted(certificatePath)
            ? certificatePath
            : Path.Combine(environment.ContentRootPath, certificatePath);

        if (!File.Exists(resolvedPath))
        {
            throw new InvalidOperationException(
                $"OpenIddict certificate file was not found at '{resolvedPath}'. Configure OpenIddict:Certificates:Path correctly.");
        }

        var certificate = new X509Certificate2(
            resolvedPath,
            certificatePassword,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);

        options.AddEncryptionCertificate(certificate)
            .AddSigningCertificate(certificate);

        return;
    }

    options.AddEphemeralEncryptionKey()
        .AddEphemeralSigningKey();
}

static SeedOptions GetSeedOptions(IConfiguration configuration)
{
    return configuration.GetSection("SeedData").Get<SeedOptions>() ?? new SeedOptions();
}

static IReadOnlyList<SeedUserDescriptor> GetSeedUsers(SeedOptions seedOptions)
{
    if (seedOptions.Users is { Count: > 0 })
    {
        return seedOptions.Users
            .Where(user => !string.IsNullOrWhiteSpace(user.UserName) && !string.IsNullOrWhiteSpace(user.Email))
            .Select(user => new SeedUserDescriptor
            {
                Id = string.IsNullOrWhiteSpace(user.Id)
                    ? Guid.CreateVersion7().ToString()
                    : user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Password = string.IsNullOrWhiteSpace(user.Password) ? "Passw0rd!" : user.Password,
                Roles = user.Roles.Count == 0 ? ["Customer"] : user.Roles
            })
            .ToArray();
    }

    return
    [
        new SeedUserDescriptor
        {
            Id = "11111111-1111-1111-1111-111111111111",
            UserName = seedOptions.UserName ?? "mcp.user",
            Email = seedOptions.Email ?? "mcp.user@tedu.local",
            Password = seedOptions.Password ?? "Passw0rd!",
            Roles = ["Customer"]
        }
    ];
}

sealed class SeedOptions
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? InspectorRedirectUri { get; set; }
    public string? Audience { get; set; }
    public List<SeedUserDescriptor>? Users { get; set; }
}

sealed class SeedUserDescriptor
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = "Passw0rd!";
    public List<string> Roles { get; set; } = [];
}

static class AuthPermissions
{
    public static class Orders
    {
        public const string View = "Permissions.Orders.View";
        public const string Cancel = "Permissions.Orders.Cancel";
        public const string Manage = "Permissions.Orders.Manage";
    }

    public static class Reports
    {
        public const string ViewRevenue = "Permissions.Reports.ViewRevenue";
    }
}
