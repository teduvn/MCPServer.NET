using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // 1. Table name
            builder.ToTable("Users");

            // 2. Primary key
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                   .ValueGeneratedNever(); // Guid do Domain tạo

            // 3. Scalar properties
            builder.Property(u => u.FullName)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(u => u.Email)
                   .HasMaxLength(255)
                   .IsRequired();

            // Index cho Email để tối ưu tìm kiếm và đảm bảo unique
            builder.HasIndex(u => u.Email)
                   .IsUnique();

            builder.Property(u => u.CreatedAt)
                   .IsRequired();

            builder.Property(u => u.IsActive)
                   .IsRequired();

            // 4. Ignore domain events (not mapped to database)
            builder.Ignore(u => u.DomainEvents);
        }
    }
}
