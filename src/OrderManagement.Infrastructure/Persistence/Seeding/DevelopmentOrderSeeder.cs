using OrderManagement.Domain.ValueObjects;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Seed đơn hàng giả để dev và test. KHÔNG dùng trong production.
    /// </summary>
    public class DevelopmentOrderSeeder(ApplicationDbContext context) : IDataSeeder
    {
        public int Order => 10; // Chạy sau — cần Customer và Product có sẵn

        public async Task SeedAsync(CancellationToken ct = default)
        {
            if (await context.Orders.AnyAsync(ct))
                return;

            // Lấy danh sách customers đã được seed
            var customers = await context.Customers
                .Select(c => new { c.Id, c.Email })
                .Take(10)
                .ToListAsync(ct);

            if (!customers.Any())
                return; // Không có customer, skip seeding

            // Seed một vài products nếu chưa có
            if (!await context.Products.AnyAsync(ct))
            {
                var products = Enumerable.Range(1, 10).Select(i =>
                    Product.Create(
                        name: $"Sample Product {i}",
                        description: $"Description for product {i}",
                        price: Money.Create(Random.Shared.Next(100, 1000) * 1000m, "VND"),
                        weightKg: Random.Shared.Next(1, 20) * 0.5m,
                        stockQuantity: Random.Shared.Next(50, 200)
                    )
                ).ToList();

                await context.Products.AddRangeAsync(products, ct);
                await context.SaveChangesAsync(ct);
            }

            // Lấy danh sách products
            var productList = await context.Products.Take(10).ToListAsync(ct);
            if (!productList.Any())
                return;

            // Tạo shipping address mẫu cho mỗi order
            var addressData = new[]
            {
                ("123 Nguyen Hue", "Ho Chi Minh", "HCM", "VN", "70000"),
                ("456 Le Loi", "Ha Noi", "HN", "VN", "10000"),
                ("789 Tran Hung Dao", "Da Nang", "DN", "VN", "50000")
            };

            var orderList = new List<Domain.Entities.Order>();
            for (int i = 1; i <= 20; i++)
            {
                // Chọn customer ngẫu nhiên
                var customer = customers[Random.Shared.Next(customers.Count)];

                // Tạo address MỚI cho mỗi order (không share reference)
                var addressTemplate = addressData[Random.Shared.Next(addressData.Length)];
                var address = Address.Create(
                    addressTemplate.Item1, 
                    addressTemplate.Item2, 
                    addressTemplate.Item3, 
                    addressTemplate.Item4, 
                    addressTemplate.Item5);

                // Debug logging
                Console.WriteLine($"Creating order {i}:");
                Console.WriteLine($"  Customer: {customer.Id} - {customer.Email}");
                Console.WriteLine($"  Address: {address.Street}, {address.City}, {address.Province}, {address.Country}");

                var orderResult = Domain.Entities.Order.CreateDraft(customer.Id, address, customer.Email);

                if (!orderResult.IsSuccess)
                {
                    Console.WriteLine($"  ERROR: {orderResult.Error}");
                    continue;
                }

                var order = orderResult.Value;

                // Verify ShippingAddress
                Console.WriteLine($"  Order created - ShippingAddress: {order.ShippingAddress?.Street ?? "NULL"}");

                // Thêm 1-3 items ngẫu nhiên
                var itemCount = Random.Shared.Next(1, 4);
                for (int j = 0; j < itemCount; j++)
                {
                    var product = productList[Random.Shared.Next(productList.Count)];
                    order.AddItem(
                        productId: product.Id,
                        productName: product.Name,
                        unitPrice: product.Price,
                        quantity: Random.Shared.Next(1, 5)
                    );
                }

                orderList.Add(order);
            }

            // Save từng order một để debug
            foreach (var order in orderList)
            {
                Console.WriteLine($"\nSaving order {order.Id}:");
                Console.WriteLine($"  Status: {order.Status}");
                Console.WriteLine($"  CustomerId: {order.CustomerId}");
                Console.WriteLine($"  CustomerEmail: {order.CustomerEmail}");
                Console.WriteLine($"  ShippingAddress: {order.ShippingAddress}");
                Console.WriteLine($"    Street: '{order.ShippingAddress.Street}'");
                Console.WriteLine($"    City: '{order.ShippingAddress.City}'");

                await context.Orders.AddAsync(order, ct);

                // Check tracked entity
                var entry = context.Entry(order);
                Console.WriteLine($"  Entity State: {entry.State}");

                var shippingAddressEntry = entry.Reference(o => o.ShippingAddress).TargetEntry;
                if (shippingAddressEntry != null)
                {
                    Console.WriteLine($"  ShippingAddress State: {shippingAddressEntry.State}");
                    foreach (var prop in shippingAddressEntry.Properties)
                    {
                        Console.WriteLine($"    {prop.Metadata.Name}: '{prop.CurrentValue}'");
                    }
                }
                else
                {
                    Console.WriteLine("  WARNING: ShippingAddress entry is NULL!");
                }

                await context.SaveChangesAsync(ct);
                Console.WriteLine("  ✓ Saved successfully");
            }
        }
    }

}
