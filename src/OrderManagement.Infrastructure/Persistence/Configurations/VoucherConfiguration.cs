using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    internal sealed class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
    {
        public void Configure(EntityTypeBuilder<Voucher> builder)
        {
            // 1. Table name
            builder.ToTable("Vouchers");

            // 2. Primary key
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id)
                   .ValueGeneratedNever(); // Guid do Domain tạo

            // 3. Scalar properties
            builder.Property(v => v.Code)
                   .HasMaxLength(50)
                   .IsRequired();

            // Index cho Code để tối ưu tìm kiếm và đảm bảo unique
            builder.HasIndex(v => v.Code)
                   .IsUnique();

            builder.Property(v => v.Type)
                   .HasConversion<string>()  // Lưu enum dạng string
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(v => v.DiscountValue)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(v => v.ValidFrom)
                   .IsRequired();

            builder.Property(v => v.ValidTo)
                   .IsRequired();

            builder.Property(v => v.UsageLimit)
                   .IsRequired();

            builder.Property(v => v.UsedCount)
                   .IsRequired();

            builder.Property(v => v.IsActive)
                   .IsRequired();

            // 4. Value Object: Money (MinimumOrderValue) - nullable
            builder.OwnsOne(v => v.MinimumOrderValue, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                            .HasColumnName("MinimumOrderValue")
                            .HasColumnType("decimal(18,2)")
                            .IsRequired();

                moneyBuilder.Property(m => m.Currency)
                            .HasColumnName("MinimumOrderCurrency")
                            .HasMaxLength(3)
                            .IsRequired();
            });

            // 5. Value Object: Money (MaximumDiscountAmount) - nullable
            builder.OwnsOne(v => v.MaximumDiscountAmount, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                            .HasColumnName("MaximumDiscountAmount")
                            .HasColumnType("decimal(18,2)")
                            .IsRequired();

                moneyBuilder.Property(m => m.Currency)
                            .HasColumnName("MaximumDiscountCurrency")
                            .HasMaxLength(3)
                            .IsRequired();
            });

            // 6. Index cho ValidFrom và ValidTo để tối ưu query tìm voucher hợp lệ
            builder.HasIndex(v => new { v.ValidFrom, v.ValidTo });

            // 7. Concurrency token
            builder.Property<uint>("RowVersion")
                   .IsRowVersion();
        }
    }
}
