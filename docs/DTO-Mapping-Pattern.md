# 📦 DTO Mapping Pattern - Clean Architecture

## 📁 Cấu trúc Thư mục

```
src/OrderManagement.Application/
└── Orders/
    ├── DTOs/                       # Data Transfer Objects
    │   ├── OrderDto.cs
    │   ├── OrderItemDto.cs
    │   └── AddressDto.cs
    │
    ├── Mappings/                   # Extension Methods cho mapping
    │   └── OrderMappingExtensions.cs
    │
    └── Queries/                    # CQRS Query Handlers
        ├── GetActiveOrdersByCustomerQueryHandler.cs
        └── GetOrderByIdQueryHandler.cs
```

---

## 🎯 Tại sao Extension Method cho Mapping?

### ✅ **Advantages**

1. **Separation of Concerns**
   - Domain không biết về DTOs
   - Application layer quản lý việc mapping
   - Giữ Domain layer pure và testable

2. **Reusability**
   ```csharp
   // Dùng trong bất kỳ query/command nào
   var dto = order.ToDto();
   var dtos = orders.Select(o => o.ToDto()).ToList();
   ```

3. **Fluent API**
   ```csharp
   // Code đọc tự nhiên hơn
   return order.ToDto();                    // ✅ Good
   return OrderMapper.Map(order);           // ⚠️ OK but verbose
   return _mapper.Map<OrderDto>(order);     // ⚠️ Requires dependency
   ```

4. **Testability**
   - Dễ unit test mapping logic riêng biệt
   - Không cần mock AutoMapper hay dependencies khác

### ❌ **Anti-patterns to Avoid**

```csharp
// ❌ KHÔNG làm thế này - Vi phạm Clean Architecture
public class Order : Entity
{
    public OrderDto ToDto()  // ❌ Domain biết về Application
    {
        return new OrderDto { ... };
    }
}
```

```csharp
// ❌ KHÔNG làm thế này - God Object
public static class MappingExtensions
{
    public static OrderDto ToDto(this Order order) { ... }
    public static CustomerDto ToDto(this Customer customer) { ... }
    public static ProductDto ToDto(this Product product) { ... }
    // 50+ other methods... ❌
}
```

---

## 🔧 Cách sử dụng

### **1. Trong Query Handler**

```csharp
public class GetActiveOrdersByCustomerQueryHandler : IRequestHandler<...>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(...)
    {
        var spec = new ActiveOrdersByCustomerSpecification(request.CustomerId);
        var orders = await _orderRepository.ListAsync(spec, ct);
        
        // 👇 Extension method tự động available
        return orders.Select(o => o.ToDto()).ToList().AsReadOnly();
    }
}
```

### **2. Mapping đơn lẻ**

```csharp
var order = await _orderRepository.GetByIdAsync(orderId);
return order?.ToDto();
```

### **3. Mapping collection**

```csharp
var orders = await _orderRepository.GetAllAsync();
return orders.Select(o => o.ToDto()).ToList();
```

---

## 🆚 So sánh với AutoMapper

| Feature | Extension Method | AutoMapper |
|---------|-----------------|------------|
| **Setup** | Không cần config | Cần Profile config |
| **Dependencies** | 0 | NuGet package |
| **Performance** | Fast (compile-time) | Slower (reflection) |
| **Debugging** | Dễ debug | Khó debug mapping errors |
| **IntelliSense** | Có đầy đủ | Limited |
| **Testability** | Dễ test | Cần mock IMapper |
| **Learning Curve** | Thấp | Trung bình |

### Khi nào dùng AutoMapper?

- ✅ Mapping phức tạp với nhiều nested objects
- ✅ Cần reverse mapping (DTO → Entity)
- ✅ Team đã quen với AutoMapper
- ✅ Project lớn với hàng trăm DTOs

### Khi nào dùng Extension Method?

- ✅ Project nhỏ/vừa
- ✅ Mapping đơn giản (Entity → DTO one-way)
- ✅ Cần performance tối ưu
- ✅ Team prefer explicit code
- ✅ Không muốn thêm dependencies

---

## 📝 Best Practices

### ✅ **DO**

```csharp
// 1. Namespace riêng cho từng feature
namespace OrderManagement.Application.Orders.Mappings

// 2. Extension method theo từng entity
public static OrderDto ToDto(this Order order)

// 3. Explicit mapping - rõ ràng từng property
return new OrderDto
{
    Id = order.Id,
    Status = order.Status.ToString(),
    // ...
};

// 4. Handle null safely
return order?.ToDto();
```

### ❌ **DON'T**

```csharp
// 1. Không mix mapping vào Domain
public class Order
{
    public OrderDto ToDto() { } // ❌
}

// 2. Không để DTOs trong Domain
namespace OrderManagement.Domain.DTOs // ❌

// 3. Không dùng dynamic/reflection
public static T ToDto<T>(this Entity entity) // ❌ Too generic

// 4. Không để business logic trong mapping
public static OrderDto ToDto(this Order order)
{
    order.CalculateTotal(); // ❌ Side effect
    return new OrderDto { ... };
}
```

---

## 🧪 Testing Strategy

```csharp
[Fact]
public void ToDto_Should_Map_Order_Correctly()
{
    // Arrange
    var order = CreateTestOrder();

    // Act
    var dto = order.ToDto();

    // Assert
    Assert.Equal(order.Id, dto.Id);
    Assert.Equal(order.Status.ToString(), dto.Status);
    // ... test all properties
}
```

---

## 🚀 Kết luận

**Extension Method pattern** là lựa chọn tốt cho:
- ✅ Clean Architecture projects
- ✅ CQRS/MediatR applications
- ✅ Teams prefer explicit code
- ✅ Performance-critical scenarios

**Ưu điểm chính:**
1. ✅ Giữ Domain layer pure
2. ✅ Code rõ ràng, dễ maintain
3. ✅ Performance tốt (compile-time)
4. ✅ Dễ test và debug
5. ✅ Không cần dependencies

---

## 📚 Related Files

- `src/OrderManagement.Application/Orders/DTOs/` - DTO definitions
- `src/OrderManagement.Application/Orders/Mappings/` - Mapping extensions
- `tests/OrderManagement.Tests/Application/Mappings/` - Mapping tests
