using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
    {
        public void Configure(EntityTypeBuilder<AppRole> builder)
        {
            builder.ToTable("AspNetRoles");

            builder.Property(r => r.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            // Index trên NormalizedName — Identity tạo sẵn, confirm lại để explicit
            builder.HasIndex(r => r.NormalizedName)
                .IsUnique()
                .HasDatabaseName("IX_AspNetRoles_NormalizedName");
        }
    }

}
