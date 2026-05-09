using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // 1. Table name
            builder.ToTable("Products");

            // 2. Primary key
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                   .ValueGeneratedNever(); // Guid do Domain tạo

            // 3. Scalar properties
            builder.Property(p => p.Name)
                   .HasMaxLength(200)
                   .IsRequired();

            // Index cho Name để tối ưu tìm kiếm
            builder.HasIndex(p => p.Name);

            builder.Property(p => p.Description)
                   .HasMaxLength(1000);

            builder.Property(p => p.WeightKg)
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            builder.Property(p => p.StockQuantity)
                   .IsRequired();

            builder.Property(p => p.IsActive)
                   .IsRequired();

            builder.Property(p => p.CreatedAt)
                   .IsRequired();

            builder.Property(p => p.UpdatedAt);

            // 4. Value Object: Money (Price)
            builder.OwnsOne(p => p.Price, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                            .HasColumnName("Price")
                            .HasColumnType("decimal(18,2)")
                            .IsRequired();

                moneyBuilder.Property(m => m.Currency)
                            .HasColumnName("PriceCurrency")
                            .HasMaxLength(3)
                            .IsRequired();
            });

            // 5. Concurrency token
            // SQLite không hỗ trợ rowversion như SQL Server, nên dùng DefaultValue
            builder.Property<uint>("RowVersion")
                   .IsRowVersion()
                   .HasDefaultValue(0u)
                   .ValueGeneratedOnAddOrUpdate();
        }
    }
}
