using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Identity
{
    /// <summary>
    /// AppUser là Identity persistence model.
    /// Chỉ dùng bởi Infrastructure — Application và Domain không biết class này.
    /// Id được đồng bộ với Domain User.Id — cùng Guid.
    /// </summary>
    public class AppUser : IdentityUser<Guid>
    {
        /// <summary>
        /// FullName được đồng bộ với Domain User.FullName.
        /// Lưu ở đây để JWT token và UserManager trả về đầy đủ thông tin.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Ngày tạo account — sync với Domain User.CreatedAt.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

}
