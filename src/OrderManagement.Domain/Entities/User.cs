using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Entities
{
    /// <summary>
    /// Domain User — đại diện cho khái niệm 'người dùng' trong business.
    /// Không biết Identity, không biết EF Core, không biết HTTP.
    /// </summary>
    public class User : Entity
    {
        public string FullName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public bool IsActive { get; private set; }

        // Private constructor — bắt buộc dùng factory method
        private User() { }

        /// <summary>
        /// Factory method — enforce invariants khi tạo User.
        /// </summary>
        public static User Create(Guid id, string fullName, string email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            return new User
            {
                Id = id,
                FullName = fullName.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        public void Deactivate() => IsActive = false;

        public void UpdateFullName(string fullName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
            FullName = fullName.Trim();
        }
    }

}
