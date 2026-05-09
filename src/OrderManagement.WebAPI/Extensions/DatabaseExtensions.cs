using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Seeding;

namespace OrderManagement.WebAPI.Extensions
{
    public static class DatabaseExtensions
    {
        public static async Task InitialiseDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Chạy pending migrations — CHỈ dùng ở dev
                // Production: apply SQL script thủ công
                if (app.Environment.IsDevelopment())
                {
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrated successfully");
                }

                // Chạy seeders theo thứ tự Order
                var seeders = scope.ServiceProvider
                    .GetServices<IDataSeeder>()
                    .OrderBy(s => s.Order);

                foreach (var seeder in seeders)
                {
                    await seeder.SeedAsync();
                    logger.LogInformation("Seeder {SeederName} completed",
                        seeder.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initialising the database");
                throw; // Fail fast — không để app chạy với DB lỗi
            }
        }
    }

}
