using OrderManagement.Domain.Common;
using OrderManagement.Domain.ValueObjects;
using System;

namespace OrderManagement.Domain.Entities
{
    /// <summary>
    /// Entity Product - đại diện cho sản phẩm trong hệ thống.
    /// </summary>
    public sealed class Product : Entity
    {
        public string Name { get; private set; } = null!;
        public string Description { get; private set; } = string.Empty;
        public Money Price { get; private set; } = null!;
        public decimal WeightKg { get; private set; }
        public int StockQuantity { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private Product() { }

        public static Product Create(
            string name,
            string description,
            Money price,
            decimal weightKg,
            int stockQuantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Tên sản phẩm không được để trống.");

            if (price.Amount <= 0)
                throw new DomainException("Giá sản phẩm phải lớn hơn 0.");

            if (weightKg <= 0)
                throw new DomainException("Trọng lượng phải lớn hơn 0.");

            if (stockQuantity < 0)
                throw new DomainException("Số lượng tồn kho không thể âm.");

            return new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Price = price,
                WeightKg = weightKg,
                StockQuantity = stockQuantity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void UpdatePrice(Money newPrice)
        {
            if (newPrice.Amount <= 0)
                throw new DomainException("Giá sản phẩm phải lớn hơn 0.");

            Price = newPrice;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStock(int quantity)
        {
            if (StockQuantity + quantity < 0)
                throw new DomainException("Không đủ hàng trong kho.");

            StockQuantity += quantity;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
