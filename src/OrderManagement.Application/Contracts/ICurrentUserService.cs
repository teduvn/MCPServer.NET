using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Contracts
{
    /// <summary>
    /// Abstraction để lấy thông tin user đang authenticated.
    /// Interface nằm ở Application — không phụ thuộc HttpContext.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// UserId từ JWT claim 'sub'. Null nếu request chưa authenticated.
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Email từ JWT claim 'email'.
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Kiểm tra user có role cụ thể không.
        /// </summary>
        bool IsInRole(string role);

        /// <summary>
        /// true nếu request đã được authenticated.
        /// </summary>
        bool IsAuthenticated { get; }
    }

}
