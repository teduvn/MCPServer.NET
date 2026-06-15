using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OrderManagement.AuthServer.Data;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

var seedSection = builder.Configuration.GetSection("SeedData");
var mcpAudience = seedSection["Audience"] ?? "order-management-mcp";
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
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
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
        options.UseReferenceAccessTokens();

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

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

app.MapGet("/connect/authorize", async (
    HttpContext httpContext,
    UserManager<IdentityUser> userManager,
    IConfiguration configuration) =>
{
    var request = httpContext.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("OpenIddict request is not available.");

    var userName = configuration["SeedData:UserName"] ?? "mcp.user";
    var user = await userManager.FindByNameAsync(userName)
        ?? await userManager.FindByEmailAsync(userName);

    if (user is null)
    {
        return Results.Problem(
            title: "Seed user is missing.",
            detail: $"Cannot authorize request because user '{userName}' does not exist.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

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

    foreach (var claim in identity.Claims)
    {
        claim.SetDestinations(Destinations.AccessToken);
    }

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(request.GetScopes());
    principal.SetResources(mcpAudience);

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

        return Results.SignIn(
            result.Principal,
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

    foreach (var claim in identity.Claims)
    {
        claim.SetDestinations(Destinations.AccessToken);
    }

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(request.GetScopes());
    principal.SetResources(mcpAudience);

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

static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
{
    using var scope = services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    await dbContext.Database.EnsureCreatedAsync();

    const string roleName = "Customer";
    if (!await roleManager.RoleExistsAsync(roleName))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed role failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }
    }

    var userName = configuration["SeedData:UserName"] ?? "mcp.user";
    var email = configuration["SeedData:Email"] ?? "mcp.user@tedu.local";
    var password = configuration["SeedData:Password"] ?? "Passw0rd!";
    var audience = configuration["SeedData:Audience"] ?? "order-management-mcp";

    var user = await userManager.FindByNameAsync(userName);
    if (user is null)
    {
        user = new IdentityUser
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111").ToString(),
            UserName = userName,
            Email = email,
            EmailConfirmed = true
        };

        var createUserResult = await userManager.CreateAsync(user, password);
        if (!createUserResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed user failed: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
        }
    }

    if (!await userManager.IsInRoleAsync(user, roleName))
    {
        var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed user role failed: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
        }
    }

    var clientId = configuration["SeedData:ClientId"] ?? "ordermanagement-mcp";
    var clientSecret = configuration["SeedData:ClientSecret"] ?? "ordermanagement-mcp-secret";
    var inspectorRedirectUri = configuration["SeedData:InspectorRedirectUri"] ?? "http://localhost:6274/oauth/callback";

    if (await applicationManager.FindByClientIdAsync(clientId) is null)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "OrderManagement MCP client"
        };

        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Introspection);
        descriptor.Permissions.Add(Permissions.GrantTypes.Password);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Email);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Profile);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Roles);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + "mcp_api");
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + audience);
        descriptor.RedirectUris.Add(new Uri(inspectorRedirectUri));
        descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

        await applicationManager.CreateAsync(descriptor);
    }
}
