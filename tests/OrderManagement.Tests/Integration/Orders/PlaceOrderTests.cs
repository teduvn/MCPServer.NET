using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using OrderManagement.Tests.Integration.Common;
using System.Net;
using System.Net.Http.Json;

namespace OrderManagement.Tests.Integration.Orders
{
    public class PlaceOrderTests : BaseIntegrationTest
    {
        public PlaceOrderTests(IntegrationTestWebFactory factory) : base(factory) { }


        [Fact]
        public async Task PlaceOrder_WithValidRequest_ShouldReturn201AndPersistToDb()
        {
            // ── Arrange ─────────────────────────────────────────────
            // Seed customer trực tiếp vào DB
            var customerId = Guid.NewGuid();
            await ExecuteDbAsync(async db =>
            {
                var customerResult = Customer.Create("Nguyen Van", "A", "a@example.com", "0123456789");
                var customer = customerResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            });


            var request = new PlaceOrderRequest
            {
                CustomerId = customerId,
                ShippingAddress = new AddressRequest
                {
                    Street = "123 Le Loi",
                    City = "Ho Chi Minh",
                    Province = "HCM",
                    Country = "VN"
                },
                Items = [new OrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    UnitPrice = 150_000m,
                    ProductName = "Test Product",
                    Currency = "VND"
                }]
            };


            // ── Act ──────────────────────────────────────────────────
            var response = await HttpClient.PostAsJsonAsync("/api/orders", request);


            // ── Assert — HTTP response ───────────────────────────────
            response.StatusCode.Should().Be(HttpStatusCode.Created);


            var body = await response.Content.ReadFromJsonAsync<Guid>();
            body!.Should().NotBeEmpty();


            // ── Assert — database state ──────────────────────────────
            // Kiểm tra database thật, không trust chỉ HTTP response
            var savedOrder = await ExecuteDbAsync(async db =>
                await db.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == body));


            savedOrder.Should().NotBeNull();
            savedOrder!.CustomerId.Should().Be(customerId);
            savedOrder.Status.Should().Be(OrderStatus.Placed);
            savedOrder.Items.Should().HaveCount(1);
            savedOrder.Items[0].Quantity.Should().Be(2);
        }


        [Fact]
        public async Task PlaceOrder_WithEmptyItems_ShouldReturn422()
        {
            // Arrange
            var request = new PlaceOrderRequest
            {
                CustomerId = Guid.NewGuid(),
                ShippingAddress = new AddressRequest
                {
                    Street = "123 Le Loi",
                    City = "HCM",
                    Province = "HCM",
                    Country = "VN"
                },
                Items = []  // Empty — validator sẽ reject
            };


            // Act
            var response = await HttpClient.PostAsJsonAsync("/api/orders", request);


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity); // 422


            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            problemDetails!.Errors.Should().ContainKey("Items");
        }


        [Fact]
        public async Task PlaceOrder_WithNonExistentCustomer_ShouldReturn400()
        {
            // Arrange
            var nonExistentCustomerId = Guid.NewGuid();
            var request = new PlaceOrderRequest
            {
                CustomerId = nonExistentCustomerId,
                ShippingAddress = new AddressRequest
                {
                    Street = "456 Nguyen Hue",
                    City = "Da Nang",
                    Province = "DN",
                    Country = "VN"
                },
                Items = [new OrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 50_000m,
                    ProductName = "Test Product",
                    Currency = "VND"
                }]
            };


            // Act
            var response = await HttpClient.PostAsJsonAsync("/api/orders", request);


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task PlaceOrder_WithMultipleItems_ShouldCalculateTotalCorrectly()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            await ExecuteDbAsync(async db =>
            {
                var customerResult = Customer.Create("Tran Thi", "B", "b@example.com", "0987654321");
                var customer = customerResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            });


            var request = new PlaceOrderRequest
            {
                CustomerId = customerId,
                ShippingAddress = new AddressRequest
                {
                    Street = "789 Tran Hung Dao",
                    City = "Ha Noi",
                    Province = "HN",
                    Country = "VN"
                },
                Items = [
                    new OrderItemRequest
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = 3,
                        UnitPrice = 100_000m,
                        ProductName = "Product A",
                        Currency = "VND"
                    },
                    new OrderItemRequest
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = 2,
                        UnitPrice = 200_000m,
                        ProductName = "Product B",
                        Currency = "VND"
                    }
                ]
            };


            // Act
            var response = await HttpClient.PostAsJsonAsync("/api/orders", request);


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);


            var orderId = await response.Content.ReadFromJsonAsync<Guid>();


            var savedOrder = await ExecuteDbAsync(async db =>
                await db.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId));


            savedOrder.Should().NotBeNull();
            savedOrder!.Items.Should().HaveCount(2);
            // Total = (3 * 100,000) + (2 * 200,000) = 700,000
            savedOrder.TotalAmount.Amount.Should().Be(700_000m);
        }


        [Fact]
        public async Task GetOrder_WithValidId_ShouldReturn200()
        {
            // Arrange - Seed customer and order
            var customerId = Guid.NewGuid();
            var orderId = Guid.NewGuid();


            await ExecuteDbAsync(async db =>
            {
                var customerResult = Customer.Create("Le Van", "C", "c@example.com", "0123456789");
                var customer = customerResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
                db.Customers.Add(customer);


                // Create order using CreateDraft then adding items
                var shippingAddress = new Address("123 Test St", "Test City", "TC", "VN", "12345");
                var orderResult = Order.CreateDraft(customerId, shippingAddress, "c@example.com");
                var order = orderResult.Value!;
                typeof(Order).GetProperty("Id")!.SetValue(order, orderId);

                // Add items to the order
                order.AddItem(Guid.NewGuid(), "Test Product", new Money(100_000m, "VND"), 2);
                order.Place();

                db.Orders.Add(order);
                await db.SaveChangesAsync();
            });


            // Act
            var response = await HttpClient.GetAsync($"/api/v1/orders/{orderId}");


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);


            var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>();
            orderDto.Should().NotBeNull();
            orderDto!.Id.Should().Be(orderId);
            orderDto.CustomerId.Should().Be(customerId);
            orderDto.Items.Should().HaveCount(1);
        }


        [Fact]
        public async Task GetOrder_WithNonExistentId_ShouldReturn404()
        {
            // Arrange
            var nonExistentOrderId = Guid.NewGuid();


            // Act
            var response = await HttpClient.GetAsync($"/api/v1/orders/{nonExistentOrderId}");


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task PlaceOrder_WithInvalidQuantity_ShouldReturn422()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            await ExecuteDbAsync(async db =>
            {
                var customerResult = Customer.Create("Pham Van", "D", "d@example.com", "0111222333");
                var customer = customerResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            });


            var request = new PlaceOrderRequest
            {
                CustomerId = customerId,
                ShippingAddress = new AddressRequest
                {
                    Street = "999 Test Street",
                    City = "Test City",
                    Province = "TC",
                    Country = "VN"
                },
                Items = [new OrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 0, // Invalid quantity
                    UnitPrice = 100_000m,
                    ProductName = "Test Product",
                    Currency = "VND"
                }]
            };


            // Act
            var response = await HttpClient.PostAsJsonAsync("/api/orders", request);


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }


        [Fact]
        public async Task GetOrder_AsOwner_WithAuthentication_ShouldReturn200()
        {
            // Arrange - Seed customer and order
            var customerId = Guid.NewGuid();
            var orderId = Guid.NewGuid();


            await ExecuteDbAsync(async db =>
            {
                var customerResult = Customer.Create("Hoang Thi", "E", "e@example.com", "0222333444");
                var customer = customerResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
                db.Customers.Add(customer);


                // Create order
                var shippingAddress = new Address("456 Owner St", "Owner City", "OC", "VN", "67890");
                var orderResult = Order.CreateDraft(customerId, shippingAddress, "e@example.com");
                var order = orderResult.Value!;
                typeof(Order).GetProperty("Id")!.SetValue(order, orderId);

                order.AddItem(Guid.NewGuid(), "Owner Product", new Money(200_000m, "VND"), 1);
                order.Place();

                db.Orders.Add(order);
                await db.SaveChangesAsync();
            });


            // Authenticate as the order owner
            AuthenticateAs(customerId, "Customer");


            // Act
            var response = await HttpClient.GetAsync($"/api/v1/orders/{orderId}");


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>();
            orderDto.Should().NotBeNull();
            orderDto!.CustomerId.Should().Be(customerId);


            // Clean up authentication for next test
            ClearAuthentication();
        }


        [Fact]
        public async Task GetOrder_AsNonOwner_WithAuthentication_ShouldStillReturn200()
        {
            // Note: Hiện tại Authorization đang bị comment, nên test này sẽ pass
            // Khi enable [Authorize(Policy = "MustOwnOrder")], test này nên return 403

            // Arrange - Seed customer and order
            var ownerId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            var orderId = Guid.NewGuid();


            await ExecuteDbAsync(async db =>
            {
                // Create owner
                var ownerResult = Customer.Create("Order", "Owner", "owner@example.com", "0333444555");
                var owner = ownerResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(owner, ownerId);
                db.Customers.Add(owner);


                // Create different user
                var userResult = Customer.Create("Different", "User", "user@example.com", "0444555666");
                var user = userResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(user, differentUserId);
                db.Customers.Add(user);


                // Create order owned by ownerId
                var shippingAddress = new Address("789 Auth St", "Auth City", "AC", "VN", "11111");
                var orderResult = Order.CreateDraft(ownerId, shippingAddress, "owner@example.com");
                var order = orderResult.Value!;
                typeof(Order).GetProperty("Id")!.SetValue(order, orderId);

                order.AddItem(Guid.NewGuid(), "Auth Product", new Money(300_000m, "VND"), 1);
                order.Place();

                db.Orders.Add(order);
                await db.SaveChangesAsync();
            });


            // Authenticate as a different user (not the owner)
            AuthenticateAs(differentUserId, "Customer");


            // Act
            var response = await HttpClient.GetAsync($"/api/v1/orders/{orderId}");


            // Assert
            // TODO: Khi enable authorization policy, đổi expectation thành Forbidden
            response.StatusCode.Should().Be(HttpStatusCode.OK); 
            // Expected when policy enabled: response.StatusCode.Should().Be(HttpStatusCode.Forbidden);


            // Clean up
            ClearAuthentication();
        }


        [Fact]
        public async Task GetOrder_WithoutAuthentication_ShouldStillReturn200()
        {
            // Note: Hiện tại [Authorize] đang bị comment, nên không cần auth
            // Khi enable authorization, test này nên return 401 Unauthorized

            // Arrange
            var customerId = Guid.NewGuid();
            var orderId = Guid.NewGuid();


            await ExecuteDbAsync(async db =>
            {
                var customerResult = Customer.Create("No", "Auth", "noauth@example.com", "0555666777");
                var customer = customerResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
                db.Customers.Add(customer);


                var shippingAddress = new Address("999 Unauth St", "Unauth City", "UC", "VN", "22222");
                var orderResult = Order.CreateDraft(customerId, shippingAddress, "noauth@example.com");
                var order = orderResult.Value!;
                typeof(Order).GetProperty("Id")!.SetValue(order, orderId);

                order.AddItem(Guid.NewGuid(), "Unauth Product", new Money(400_000m, "VND"), 1);
                order.Place();

                db.Orders.Add(order);
                await db.SaveChangesAsync();
            });


            // No authentication set


            // Act
            var response = await HttpClient.GetAsync($"/api/v1/orders/{orderId}");


            // Assert
            // TODO: Khi enable [Authorize], đổi expectation thành Unauthorized
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            // Expected when auth enabled: response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }


        [Fact]
        public async Task PlaceOrder_AsAdmin_WithAuthentication_ShouldReturn201()
        {
            // Arrange - Seed customer
            var customerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();

            await ExecuteDbAsync(async db =>
            {
                var customerResult = Customer.Create("Admin", "Test", "admin@example.com", "0666777888");
                var customer = customerResult.Value!;
                typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            });


            var request = new PlaceOrderRequest
            {
                CustomerId = customerId,
                ShippingAddress = new AddressRequest
                {
                    Street = "111 Admin St",
                    City = "Admin City",
                    Province = "AD",
                    Country = "VN"
                },
                Items = [new OrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 5,
                    UnitPrice = 500_000m,
                    ProductName = "Admin Product",
                    Currency = "VND"
                }]
            };


            // Authenticate as Admin
            AuthenticateAs(adminId, "Admin");


            // Act
            var response = await HttpClient.PostAsJsonAsync("/api/orders", request);


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);


            // Clean up
            ClearAuthentication();
        }

    }

}
