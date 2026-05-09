using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace OrderManagement.WebAPI.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddAuthorization(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                                                       Encoding.UTF8.GetBytes(secretKey))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        // Không có token hoặc token hết hạn
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();  // Ngăn response mặc định

                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/problem+json";

                            var pd = new ProblemDetails
                            {
                                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                                Title = "Unauthorized",
                                Status = 401,
                                Detail = "Token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.",
                                Instance = context.Request.Path
                            };

                            await context.Response.WriteAsJsonAsync(pd);
                        },

                        // Token hợp lệ nhưng không đủ quyền
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/problem+json";

                            var pd = new ProblemDetails
                            {
                                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                                Title = "Forbidden",
                                Status = 403,
                                Detail = "Bạn không có quyền truy cập resource này.",
                                Instance = context.Request.Path
                            };

                            await context.Response.WriteAsJsonAsync(pd);
                        }
                    };
                });

            return services;
        }
    }

}
