using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories;

namespace OrderManagement.Tests.Unit.Infrastructure.Persistence.Repositories
{
    public class OrderRepositoryTests : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        private ApplicationDbContext _context = null!;
        private OrderRepository _repository = null!;


        // IAsyncLifetime cho phép setup async trước mỗi test class
        public async Task InitializeAsync()
        {
            // SQLite in-memory: phải giữ connection sống
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();


            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)  // Dùng SQLite, không phải EF In-Memory
                .Options;


            // Tạo mock IPublisher — không cần MediatR thật
            var publisher = Substitute.For<IPublisher>();
            _context = new ApplicationDbContext(options, publisher);
            await _context.Database.EnsureCreatedAsync();  // tạo schema


            _repository = new OrderRepository(_context);
        }

        [Fact]
        public async Task Add_ValidOrder_PersistsToDatabase()
        {
            // Arrange: Seed Product vào DB trước
            var productId = Guid.NewGuid();
            var product = Product.Create(
                "Product A",
                "Test Product",
                Money.FromVND(150_000m),
                1.0m,
                100);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var customerId = Guid.NewGuid();
            var address = Address.Create("123 Main St", "Ho Chi Minh", "HCM", "VN", "70000");

            var orderResult = Order.CreateDraft(customerId, address);
            orderResult.IsSuccess.Should().BeTrue();
            var order = orderResult.Value;

            // Add items using public AddItem method
            order.AddItem(product.Id, "Product A", Money.FromVND(150_000m), 2);

            // Act
            _repository.Add(order);
            await _context.SaveChangesAsync();  // Commit trực tiếp trong test

            // Assert
            var loaded = await _repository.GetByIdAsync(order.Id);
            loaded.Should().NotBeNull();
            loaded!.CustomerId.Should().Be(customerId);
            loaded.Items.Should().HaveCount(1);
        }


        [Fact]
        public async Task GetByIdAsync_NonExistentId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());


            // Assert
            result.Should().BeNull();
        }

        [Fact(Skip = "SQLite in-memory không hỗ trợ RowVersion/Optimistic Concurrency giống SQL Server")]
        public async Task Update_ConcurrentEdit_ThrowsDbUpdateConcurrencyException()
        {
            // Arrange: Seed Product vào DB trước
            var product = Product.Create(
                "Product A",
                "Test Product",
                Money.FromVND(100m),
                1.0m,
                100);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Arrange: seed một Order vào DB
            var customerId = Guid.NewGuid();
            var address = Address.Create("123 Main St", "Ho Chi Minh", "HCM", "VN", "70000");

            var orderResult = Order.CreateDraft(customerId, address);
            var order = orderResult.Value;
            order.AddItem(product.Id, "Product A", Money.FromVND(100m), 1);

            _repository.Add(order);
            await _context.SaveChangesAsync();

            // Detach the order from context1 to simulate disconnected scenario
            _context.Entry(order).State = EntityState.Detached;

            // Simulate: người dùng thứ 2 load và sửa Order từ context khác
            var options2 = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection).Options;
            var publisher2 = Substitute.For<IPublisher>();
            await using var context2 = new ApplicationDbContext(options2, publisher2);

            var orderInContext2 = await context2.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == order.Id);
            var newAddress1 = Address.Create("New Street", "New City", "NC", "VN", "12345");
            orderInContext2!.UpdateShippingAddress(newAddress1);
            await context2.SaveChangesAsync(); // Context2 commit trước

            // Act: context1 cũng cố update Order với RowVersion cũ
            var newAddress2 = Address.Create("Another Street", "Another City", "AC", "VN", "99999");
            order.UpdateShippingAddress(newAddress2);
            _context.Orders.Update(order);

            // Assert: phải throw vì RowVersion mismatch
            // NOTE: Test này chỉ hoạt động với SQL Server, không hoạt động với SQLite in-memory
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _context.SaveChangesAsync());
        }


        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

}
