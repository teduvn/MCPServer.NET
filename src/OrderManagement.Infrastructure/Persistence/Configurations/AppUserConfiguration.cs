using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            // Giữ tên bảng mặc định của Identity để tương thích tool ecosystem
            // (Azure AD B2C, Identity Server, v.v. đều expect AspNetUsers)
            builder.ToTable("AspNetUsers");

            // Custom columns
            builder.Property(u => u.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .HasColumnType("datetime2")
                .IsRequired();

            // Email index — Identity tạo sẵn nhưng explicit cho rõ ràng
            builder.HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("IX_AspNetUsers_NormalizedEmail");

            // Ignore navigation properties không dùng trong OMS
            // (tránh EF Core eager-load không cần thiết)
            builder.Ignore(u => u.PhoneNumber);
            builder.Ignore(u => u.PhoneNumberConfirmed);
            builder.Ignore(u => u.TwoFactorEnabled);
        }
    }

}
