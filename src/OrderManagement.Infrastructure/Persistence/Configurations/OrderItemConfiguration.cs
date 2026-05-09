using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id)
                   .ValueGeneratedNever();

            builder.Property(i => i.ProductId)
                   .IsRequired();

            builder.Property(i => i.Quantity)
                   .IsRequired();

            // Value Object: UnitPrice (Money)
            builder.OwnsOne(i => i.UnitPrice, moneyBuilder =>
            {
                moneyBuilder.Property(m => m.Amount)
                            .HasColumnName("UnitPrice")
                            .HasColumnType("decimal(18,2)")
                            .IsRequired();

                moneyBuilder.Property(m => m.Currency)
                            .HasColumnName("UnitPriceCurrency")
                            .HasMaxLength(3)
                            .IsRequired();
            });
        }
    }

}
