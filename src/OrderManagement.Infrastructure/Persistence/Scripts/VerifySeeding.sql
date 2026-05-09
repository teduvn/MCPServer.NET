-- Script để verify seeding đã chạy thành công

-- Kiểm tra số lượng Customers
SELECT 'Customers' AS TableName, COUNT(*) AS RecordCount FROM Customers;

-- Kiểm tra số lượng Products  
SELECT 'Products' AS TableName, COUNT(*) AS RecordCount FROM Products;

-- Kiểm tra số lượng Orders
SELECT 'Orders' AS TableName, COUNT(*) AS RecordCount FROM Orders;

-- Kiểm tra số lượng OrderItems
SELECT 'OrderItems' AS TableName, COUNT(*) AS RecordCount FROM OrderItems;

-- Xem chi tiết Customers
SELECT TOP 5
    Id,
    FirstName,
    LastName,
    Email,
    PhoneNumber,
    Tier,
    CreatedAt,
    IsActive,
    RowVersion
FROM Customers
ORDER BY CreatedAt DESC;

-- Xem chi tiết Orders với Customer info
SELECT TOP 5
    o.Id AS OrderId,
    o.CustomerId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    c.Email AS CustomerEmail,
    o.Status,
    o.TotalAmount,
    o.Currency,
    o.CreatedAt
FROM Orders o
INNER JOIN Customers c ON o.CustomerId = c.Id
ORDER BY o.CreatedAt DESC;
