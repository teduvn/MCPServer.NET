using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrderManagement.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace OrderManagement.Tests.Integration.Common
{
    /// <summary>
    /// Base class cho tất cả integration test.
    /// IClassFixture đảm bảo một IntegrationTestWebFactory được share cho cả class.
    /// </summary>
    public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebFactory>
    {
        protected readonly HttpClient HttpClient;
        protected readonly IntegrationTestWebFactory Factory;


        protected BaseIntegrationTest(IntegrationTestWebFactory factory)
        {
            Factory = factory;


            // CreateClient() trả về HttpClient trỏ vào in-memory test server
            HttpClient = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false  // Test redirect behavior rõ ràng hơn
            });
        }


        /// <summary>
        /// Helper seed data trực tiếp vào DB (bypass HTTP layer)
        /// Dùng trong Arrange khi cần data phức tạp
        /// </summary>
        protected async Task<T> ExecuteDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await action(db);
        }


        protected async Task ExecuteDbAsync(Func<ApplicationDbContext, Task> action)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await action(db);
        }

        public async Task InitializeAsync() => await Factory.InitializeAsync();


        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Clear authentication header - useful for testing unauthorized access
        /// </summary>
        protected void ClearAuthentication()
        {
            HttpClient.DefaultRequestHeaders.Authorization = null;
        }

        // Trong BaseIntegrationTest — thêm helper authentication
        protected void AuthenticateAs(Guid userId, string role = "Customer")
        {
            // Tạo JWT token với claim cụ thể
            var token = GenerateTestToken(userId, role);
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }


        private string GenerateTestToken(Guid userId, string role)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("your-very-long-secret-key-min-32-chars"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };


            var token = new JwtSecurityToken(
                issuer: "TestIssuer",
                audience: "TestAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);


            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }

}
