using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // 1. Table name và schema
            builder.ToTable("Orders");

            // 2. Primary key — EF Core tự detect nếu tên là Id, nhưng explicit tốt hơn
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id)
                   .ValueGeneratedNever(); // Guid do Domain tạo, không để DB generate

            // 3. Scalar properties
            builder.Property(o => o.CustomerId)
                   .IsRequired();

            builder.Property(o => o.Status)
                   .HasConversion<string>()  // Lưu enum dạng string, không phải số nguyên
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(o => o.CreatedAt)
                   .IsRequired();

            // 4. Value Object: Money (TotalAmount)
            //    OwnsOne = Money không có bảng riêng, các column nằm trong bảng Orders
            builder.OwnsOne(o => o.TotalAmount, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                            .HasColumnName("TotalAmount")   // tên column rõ ràng
                            .HasColumnType("decimal(18,2)")
                            .IsRequired();

                moneyBuilder.Property(m => m.Currency)
                            .HasColumnName("Currency")
                            .HasMaxLength(3)
                            .IsRequired();
            });

            // 5. Value Object: Address (ShippingAddress)
            builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
            {
                addressBuilder.Property(a => a.Street)
                              .HasColumnName("ShippingStreet")
                              .HasMaxLength(200)
                              .IsRequired();

                addressBuilder.Property(a => a.City)
                              .HasColumnName("ShippingCity")
                              .HasMaxLength(100)
                              .IsRequired();

                addressBuilder.Property(a => a.Province)
                              .HasColumnName("ShippingProvince")
                              .HasMaxLength(100)
                              .IsRequired();

                addressBuilder.Property(a => a.Country)
                              .HasColumnName("ShippingCountry")
                              .HasMaxLength(2)
                              .IsRequired();

                addressBuilder.Property(a => a.PostalCode)
                              .HasColumnName("ShippingPostalCode")
                              .HasMaxLength(10);
            });

            // 6. Navigation đến OrderItem collection
            //    HasMany/WithOne thiết lập quan hệ 1-N
            builder.HasMany(o => o.Items)
                   .WithOne()
                   .HasForeignKey("OrderId")   // shadow property — OrderItem không có OrderId public
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);

            // 7. Concurrency token (optional — dùng cho optimistic concurrency)
            // Đánh dấu RowVersion là concurrency token
            // EF Core sẽ tự thêm WHERE RowVersion = @original vào mọi UPDATE/DELETE
            // SQLite không hỗ trợ rowversion như SQL Server, nên dùng DefaultValue
            builder.Property(o => o.RowVersion)
                .IsRowVersion()    // tương đương IsTimestamp() + IsConcurrencyToken()
                .HasDefaultValue(new byte[] { 0 })
                .ValueGeneratedOnAddOrUpdate();  // SQLite cần config này để generate RowVersion
        }
    }

}
