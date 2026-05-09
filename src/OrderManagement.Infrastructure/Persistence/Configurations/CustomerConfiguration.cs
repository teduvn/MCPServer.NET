using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            // 1. Table name
            builder.ToTable("Customers");

            // 2. Primary key
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                   .ValueGeneratedNever(); // Guid do Domain tạo

            // 3. Scalar properties
            builder.Property(c => c.FirstName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(c => c.LastName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(c => c.Email)
                   .HasMaxLength(255)
                   .IsRequired();

            // Index cho Email để tối ưu tìm kiếm và đảm bảo unique
            builder.HasIndex(c => c.Email)
                   .IsUnique();

            builder.Property(c => c.PhoneNumber)
                   .HasMaxLength(20);

            builder.Property(c => c.Tier)
                   .HasConversion<string>()  // Lưu enum dạng string
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(c => c.CreatedAt)
                   .IsRequired();

            builder.Property(c => c.UpdatedAt);

            builder.Property(c => c.IsActive)
                   .IsRequired();

            // 4. Value Object: Address (BillingAddress) - nullable
            builder.OwnsOne(c => c.BillingAddress, addressBuilder =>
            {
                addressBuilder.Property(a => a.Street)
                              .HasColumnName("BillingStreet")
                              .HasMaxLength(200)
                              .IsRequired();

                addressBuilder.Property(a => a.City)
                              .HasColumnName("BillingCity")
                              .HasMaxLength(100)
                              .IsRequired();

                addressBuilder.Property(a => a.Province)
                              .HasColumnName("BillingProvince")
                              .HasMaxLength(100)
                              .IsRequired();

                addressBuilder.Property(a => a.Country)
                              .HasColumnName("BillingCountry")
                              .HasMaxLength(2)
                              .IsRequired();

                addressBuilder.Property(a => a.PostalCode)
                              .HasColumnName("BillingPostalCode")
                              .HasMaxLength(10);
            });

            // 5. Concurrency token
            builder.Property<uint>("RowVersion")
                   .IsRowVersion()
                   .HasDefaultValue(0u);
        }
    }
}
