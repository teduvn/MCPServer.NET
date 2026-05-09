using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Seed khách hàng giả để dev và test. KHÔNG dùng trong production.
    /// </summary>
    public class DevelopmentCustomerSeeder(ApplicationDbContext context) : IDataSeeder
    {
        public int Order => 5; // Chạy trước DevelopmentOrderSeeder (order 10)

        public async Task SeedAsync(CancellationToken ct = default)
        {
            if (await context.Customers.AnyAsync(ct))
                return; // Đã có customer, không seed thêm

            var customers = new List<Customer>();

            // Tạo 10 khách hàng mẫu
            var firstNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Võ", "Đặng", "Bùi", "Đỗ", "Vũ" };
            var lastNames = new[] { "Văn A", "Thị B", "Văn C", "Thị D", "Văn E", "Thị F", "Văn G", "Thị H", "Văn I", "Thị K" };
            var cities = new[] 
            { 
                ("Ho Chi Minh", "HCM", "70000"),
                ("Ha Noi", "HN", "10000"),
                ("Da Nang", "DN", "50000"),
                ("Can Tho", "CT", "90000"),
                ("Hai Phong", "HP", "18000")
            };

            for (int i = 0; i < 10; i++)
            {
                var city = cities[i % cities.Length];
                var address = Address.Create(
                    street: $"{Random.Shared.Next(1, 999)} Nguyen Hue Street",
                    city: city.Item1,
                    province: city.Item2,
                    country: "VN",
                    postalCode: city.Item3
                );

                var customerResult = Customer.Create(
                    firstName: firstNames[i],
                    lastName: lastNames[i],
                    email: $"customer{i + 1}@example.com",
                    phoneNumber: $"0{Random.Shared.Next(900000000, 999999999)}",
                    billingAddress: address
                );

                if (customerResult.IsSuccess)
                {
                    customers.Add(customerResult.Value);
                }
            }

            await context.Customers.AddRangeAsync(customers, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}
