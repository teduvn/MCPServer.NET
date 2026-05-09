using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Exceptions
{
    /// <summary>
    /// Business rule bị vi phạm — lỗi do logic nghiệp vụ.
    /// Khác với validation error (input sai format),
    /// đây là input hợp lệ nhưng không thỏa điều kiện domain.
    /// → HTTP 400 Bad Request
    /// Ví dụ: Hủy order đã shipped, thêm item vào order đã closed.
    /// </summary>
    public class DomainException : AppException
    {
        public DomainException(string message) : base(message) { }
    }

}
