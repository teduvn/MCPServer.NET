using OrderManagement.Domain.Common;
using OrderManagement.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Entities
{
    public sealed class OrderItem : Entity
    {
        public Guid OrderId { get; private set; }
        public Guid ProductId { get; private set; }
        public string ProductName { get; private set; } = null!;
        public Money UnitPrice { get; private set; } = null!;
        public int Quantity { get; private set; }

        // Navigation property — EF Core sẽ populate
        public Product Product { get; private set; } = null!;

        // Computed property — Subtotal là kết quả tính từ UnitPrice và Quantity
        // Đây là behaviour của domain, không lưu vào DB
        public Money Subtotal => UnitPrice.Multiply(Quantity);

        private OrderItem() { }

        internal static OrderItem Create(
            Guid orderId, Guid productId,
            string productName, Money unitPrice, int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity phải lớn hơn 0");
            if (unitPrice.Amount <= 0)
                throw new DomainException("Giá phải lớn hơn 0");

            // internal — chỉ Order mới tạo được OrderItem
            return new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = productId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = quantity
            };
        }

        // internal — chỉ Order gọi được
        internal void IncreaseQuantity(int amount)
        {
            if (amount <= 0)
                throw new DomainException("Số lượng tăng thêm phải lớn hơn 0.");
            Quantity += amount;
        }
    }

}
