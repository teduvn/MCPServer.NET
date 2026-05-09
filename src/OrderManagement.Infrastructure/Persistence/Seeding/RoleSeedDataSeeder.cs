using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Identity;

namespace OrderManagement.Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Seed dữ liệu Role vào database.
    /// Idempotent: kiểm tra trước khi insert, không tạo duplicate.
    /// </summary>
    public class RoleSeedDataSeeder(ApplicationDbContext context) : IDataSeeder
    {
        public int Order => 1; // Chạy trước — Role cần có trước Permission

        public async Task SeedAsync(CancellationToken ct = default)
        {
            // Chỉ seed nếu bảng rỗng — kiểm tra AnyAsync thay vì Count
            if (await context.Roles.AnyAsync(ct))
                return; // Đã có data, không làm gì thêm

            var roles = new[]
            {
                new AppRole { Id = Guid.Parse("25313ed6-c08b-4012-b3e1-3b841f77a939"), Name = "Admin", NormalizedName = "ADMIN" },
                new AppRole { Id = Guid.Parse("1cf28b17-4cae-4710-95b8-2157e5ba4ccc"), Name = "Manager", NormalizedName = "MANAGER" },
                new AppRole { Id = Guid.Parse("ce7ded3e-a11d-408d-b5aa-e9caea44d28d"), Name = "Staff", NormalizedName = "STAFF" }
            };

            await context.Roles.AddRangeAsync(roles, ct);
            await context.SaveChangesAsync(ct);
        }
    }

}
