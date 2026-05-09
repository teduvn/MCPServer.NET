using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Common
{
    // record struct — immutable, so sánh theo giá trị
    public record Error(string Code, string Description)
    {
        // Sentinel value cho trường hợp không có lỗi
        public static readonly Error None = new(string.Empty, string.Empty);

        // Các loại lỗi chuẩn - sử dụng Code để phân biệt
        public static readonly Error BusinessRule = new("Error.BusinessRule", "A business rule violation occurred");
        public static readonly Error NotFound = new("Error.NotFound", "The requested resource was not found");
        public static readonly Error Validation = new("Error.Validation", "A validation error occurred");

        // Tiện ích: tạo nhanh không cần gọi new
        public static Error Create(string code, string description)
            => new(code, description);
    }

}
