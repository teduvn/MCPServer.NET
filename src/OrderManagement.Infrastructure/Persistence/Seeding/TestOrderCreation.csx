// Test script để debug Order creation với Address
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;

Console.WriteLine("=== Test Order Creation with Address ===\n");

// Tạo Address
var address = Address.Create("123 Test Street", "Ho Chi Minh", "HCM", "VN", "70000");
Console.WriteLine($"Address created: {address}");
Console.WriteLine($"  Street: '{address.Street}'");
Console.WriteLine($"  City: '{address.City}'");
Console.WriteLine($"  Province: '{address.Province}'");
Console.WriteLine($"  Country: '{address.Country}'");
Console.WriteLine($"  PostalCode: '{address.PostalCode}'");
Console.WriteLine();

// Tạo Order
var customerId = Guid.NewGuid();
var customerEmail = "test@example.com";
var orderResult = Order.CreateDraft(customerId, address, customerEmail);

if (orderResult.IsSuccess)
{
    var order = orderResult.Value;
    Console.WriteLine($"Order created successfully!");
    Console.WriteLine($"  OrderId: {order.Id}");
    Console.WriteLine($"  CustomerId: {order.CustomerId}");
    Console.WriteLine($"  CustomerEmail: '{order.CustomerEmail}'");
    Console.WriteLine($"  ShippingAddress: {order.ShippingAddress}");
    Console.WriteLine($"    - Street: '{order.ShippingAddress.Street}'");
    Console.WriteLine($"    - City: '{order.ShippingAddress.City}'");
    Console.WriteLine($"    - Province: '{order.ShippingAddress.Province}'");
    Console.WriteLine($"    - Country: '{order.ShippingAddress.Country}'");
    Console.WriteLine($"    - PostalCode: '{order.ShippingAddress.PostalCode}'");
    Console.WriteLine();

    // Kiểm tra null
    Console.WriteLine("Null checks:");
    Console.WriteLine($"  ShippingAddress is null: {order.ShippingAddress == null}");
    Console.WriteLine($"  ShippingAddress.Street is null: {order.ShippingAddress.Street == null}");
    Console.WriteLine($"  ShippingAddress.City is null: {order.ShippingAddress.City == null}");
}
else
{
    Console.WriteLine($"Error creating order: {orderResult.Error}");
}

Console.WriteLine("\n=== Test Completed ===");
