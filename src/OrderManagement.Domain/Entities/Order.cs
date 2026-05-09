using OrderManagement.Domain.Common;
using OrderManagement.Domain.Events;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Entities
{
    public sealed class Order : Entity
    {
        private readonly List<OrderItem> _items = new();

        // Private setters — chỉ thay đổi qua method, không assign trực tiếp từ ngoài
        public Guid CustomerId { get; private set; }
        public Address ShippingAddress { get; private set; } = null!;
        public string CustomerEmail { get; init; } = string.Empty;
        public OrderStatus Status { get; private set; }
        public Money TotalAmount { get; private set; } = null!;
        public string Currency { get; private set; } = "VND";
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Read-only collection — caller không thể modify list trực tiếp
        public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

        // EF Core cần constructor không tham số (private để không expose)
        private Order() { }

        // Factory method for creating draft order without items
        public static Result<Order> CreateDraft(Guid customerId, Address shippingAddress, string customerEmail = "")
        {
            ArgumentException.ThrowIfNullOrEmpty(customerId.ToString());
            if (customerId == Guid.Empty)
                return OrderErrors.CustomerNotFound(customerId);
            if (shippingAddress is null)
                return OrderErrors.ShippingAddressNotFound(shippingAddress);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ShippingAddress = shippingAddress,
                CustomerEmail = customerEmail,
                Status = OrderStatus.Draft,
                TotalAmount = Money.ZeroOf("VND"),
                CreatedAt = DateTime.UtcNow
            };

            return order;
        }

        // Factory method thay vì constructor public — kiểm soát invariant
        public static Result<Order> Create(Guid customerId, Address shippingAddress, IEnumerable<OrderItem> items)
        {
            ArgumentException.ThrowIfNullOrEmpty(customerId.ToString());
            if (customerId == Guid.Empty)
                return OrderErrors.CustomerNotFound(customerId);
            if (shippingAddress is null)
                return OrderErrors.ShippingAddressNotFound(shippingAddress);

            var itemsList = items?.ToList() ?? new List<OrderItem>();
            if (itemsList.Count == 0)
                return OrderErrors.EmptyItems;
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ShippingAddress = shippingAddress,
                Status = OrderStatus.Draft,
                TotalAmount = Money.ZeroOf("VND"),  // default VND, sẽ recalculate
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in itemsList)
                order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);

            // Raise event — Order tự quyết định khi nào raise
            order.RaiseDomainEvent(new OrderPlacedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                CustomerEmail = order.CustomerEmail,
                TotalAmount = order.TotalAmount.Amount,
                Items = order._items.Select(i => new OrderItemSnapshot(
                    i.ProductId, i.ProductName, i.Quantity, i.UnitPrice.Amount
                )).ToList()
            });


            return order;   
        }

        // Business method — thêm item qua Order, không tạo OrderItem trực tiếp
        public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
        {
            EnsureOrderIsModifiable();

            if (Status != OrderStatus.Draft)
                throw new DomainException("Chỉ có thể thêm item vào đơn hàng ở trạng thái Draft.");

            if (quantity <= 0)
                throw new DomainException("Số lượng phải lớn hơn 0.");

            // Nếu đã có product này, tăng quantity thay vì thêm dòng mới
            var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.IncreaseQuantity(quantity);
            }
            else
            {
                var item = OrderItem.Create(Id, productId, productName, unitPrice, quantity);
                _items.Add(item);
            }

            RecalculateTotal();
        }

        public void RemoveItem(Guid orderItemId)
        {
            EnsureOrderIsModifiable();

            if (Status != OrderStatus.Draft)
                throw new DomainException("Không thể xoá item khỏi đơn đã được xử lý.");

            var item = _items.FirstOrDefault(i => i.Id == orderItemId)
                ?? throw new NotFoundException($"Không tìm thấy item {orderItemId}.");

            _items.Remove(item);
            RecalculateTotal();
        }

        public void Place()
        {
            if (Status != OrderStatus.Draft)
                throw new DomainException("Chỉ có thể đặt đơn hàng ở trạng thái Draft.");

            if (!_items.Any())
                throw new DomainException("Đơn hàng phải có ít nhất một sản phẩm.");

            Status = OrderStatus.Placed;
            UpdatedAt = DateTime.UtcNow;
            // Domain event sẽ được cover ở bài 2.3
            //RaiseDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount));

        }

        public void Ship(string trackingNumber, DateTime estimatedDelivery)
        {
            if (Status != OrderStatus.Confirmed)
                throw new DomainException("Chỉ confirmed order mới được ship");

            Status = OrderStatus.Shipped;

            RaiseDomainEvent(new OrderShippedEvent
            {
                OrderId = Id,
                CustomerId = CustomerId,
                CustomerEmail = CustomerEmail,
                TrackingNumber = trackingNumber,
                EstimatedDelivery = estimatedDelivery
            });
        }

        public void UpdateShippingAddress(Address newAddress)
        {
            EnsureOrderIsModifiable();

            if (newAddress is null)
                throw new DomainException("Địa chỉ giao hàng không được null.");

            ShippingAddress = newAddress;
            UpdatedAt = DateTime.UtcNow;
        }


        private void RecalculateTotal()
        {
            TotalAmount = _items.Aggregate(
                Money.ZeroOf("VND"),
                (sum, item) => sum.Add(item.Subtotal));
        }

        // ======================================================
        // PRIVATE HELPER - enforce invariant tập trung
        // ======================================================
        private void EnsureOrderIsModifiable()
        {
            if (Status is OrderStatus.Shipped or OrderStatus.Cancelled)
                throw new DomainException(
                    $"Không thể chỉnh sửa Order ở trạng thái {Status}");
        }

    }

    public enum OrderStatus
    {
        Draft = 0,
        Placed = 1,
        Confirmed = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }

}
