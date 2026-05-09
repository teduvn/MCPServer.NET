using Microsoft.AspNetCore.Authorization;
using OrderManagement.WebAPI.Authorization;

namespace OrderManagement.WebAPI.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<IAuthorizationHandler, MustOwnOrderHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("MustOwnOrder", policy =>
                    policy.Requirements.Add(new MustOwnOrderRequirement()));

                // Policy kết hợp role + custom requirement
                options.AddPolicy("AdminOrOwner", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new MustOwnOrderRequirement());
                });
            });

            return services;
        }
    }
}
