using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Seed admin user mặc định dùng UserManager.
    /// Chạy sau RoleSeedDataSeeder (Order = 2 > Order = 1).
    /// Idempotent: kiểm tra email trước khi tạo.
    /// </summary>
    public class AdminUserSeeder(
        UserManager<AppUser> userManager,
        ApplicationDbContext context,
        ILogger<AdminUserSeeder> logger) : IDataSeeder
    {
        // Hardcoded Guid — idempotent, không đổi mỗi lần chạy
        private static readonly Guid AdminUserId =
            Guid.Parse("00000000-0000-0000-0000-000000000001");

        private const string AdminEmail = "admin@orderms.local";
        private const string AdminPassword = "Admin@123456"; // Đổi trước khi production!
        private const string AdminFullName = "System Administrator";

        public int Order => 2; // Chạy sau RoleSeedDataSeeder (Order = 1)

        public async Task SeedAsync(CancellationToken ct = default)
        {
            // Kiểm tra đã có admin chưa
            var existing = await userManager.FindByEmailAsync(AdminEmail);
            if (existing is not null)
            {
                logger.LogInformation("Admin user đã tồn tại, bỏ qua seeding.");
                return;
            }

            // Tạo AppUser qua UserManager
            var appUser = new AppUser
            {
                Id = AdminUserId,
                UserName = AdminEmail,
                Email = AdminEmail,
                FullName = AdminFullName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(appUser, AdminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Không thể tạo admin user: {Errors}", errors);
                return;
            }

            // Gán role Admin — role này đã được seed ở RoleSeedDataSeeder
            await userManager.AddToRoleAsync(appUser, "Admin");

            // Tạo Domain User tương ứng với cùng Id
            var domainUser = User.Create(AdminUserId, AdminFullName, AdminEmail);
            await context.Set<User>().AddAsync(domainUser, ct);
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Admin user đã được tạo thành công.");
        }
    }

}
