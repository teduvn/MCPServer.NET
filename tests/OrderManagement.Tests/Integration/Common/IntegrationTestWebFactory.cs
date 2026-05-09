using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Infrastructure.Persistence;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace OrderManagement.Tests.Integration.Common
{
    /// <summary>
    /// WebApplicationFactory tùy chỉnh: thay database thật bằng SQL Server trong Testcontainer.
    /// Implement IAsyncLifetime để start/stop container cùng với test lifecycle.
    /// </summary>
    public class IntegrationTestWebFactory
        : WebApplicationFactory<Program>, IAsyncLifetime
    {
        // Testcontainers sẽ khởi động một SQL Server container
        private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Integration@Test123") // Phải đủ mạnh theo policy SQL Server
            .Build();


        // Respawn sẽ dùng connection string này để reset database
        public string ConnectionString => _dbContainer.GetConnectionString();
        private Respawner _respawner = default!;


        /// <summary>
        /// Override ConfigureWebHost để replace connection string bằng Testcontainer's string.
        /// Chạy sau khi Docker container đã up (trong InitializeAsync).
        /// </summary>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Xóa DbContext registration gốc từ app
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);


                // Đăng ký lại với connection string của Testcontainer
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(ConnectionString));
            });
        }


        // Khởi động Docker container trước khi test chạy
        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();


            // Chạy migration để tạo schema
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            // Khởi tạo Respawner sau khi migration xong
            // Respawn sẽ đọc schema và lập kế hoạch xóa đúng thứ tự
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                // Chỉ định các bảng cần giữ lại (seed data cố định)
                TablesToIgnore = [new Table("__EFMigrationsHistory")]
            });

        }


        // Dọn dẹp container sau khi tất cả test trong class chạy xong
        public new async Task DisposeAsync()
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await _respawner.ResetAsync(conn);
            await _dbContainer.StopAsync();
        }
    }

}
