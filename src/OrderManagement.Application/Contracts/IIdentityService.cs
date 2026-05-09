using OrderManagement.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Contracts
{
    public interface IIdentityService
    {
        /// <summary>
        /// Tạo user mới — trả về userId nếu thành công.
        /// </summary>
        Task<Result<Guid>> CreateUserAsync(
            string email,
            string fullName,
            string password,
            CancellationToken ct = default);

        /// <summary>
        /// Kiểm tra email/password — trả về userId nếu đúng.
        /// </summary>
        Task<Result<Guid>> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách roles của user.
        /// </summary>
        Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId);

        /// <summary>
        /// Gán role cho user.
        /// </summary>
        Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken ct = default);
    }

}
