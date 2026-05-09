using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    /// <summary>
    /// Interface cho distributed cache.
    /// Application Layer chỉ biết interface này — không biết Redis tồn tại.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Lấy giá trị từ cache theo key.
        /// Trả về null nếu key không tồn tại hoặc đã hết TTL.
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
            where T : class;

        /// <summary>
        /// Set giá trị vào cache với TTL tùy chọn.
        /// Nếu không truyền expiry, dùng default TTL của implementation.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null,
            CancellationToken ct = default)
            where T : class;

        /// <summary>
        /// Xóa một key khỏi cache.
        /// Không throw exception nếu key không tồn tại.
        /// </summary>
        Task RemoveAsync(string key, CancellationToken ct = default);

        /// <summary>
        /// Xóa tất cả key theo prefix — dùng khi invalidate theo pattern.
        /// Ví dụ: RemoveByPrefixAsync('orders:') xóa orders:abc, orders:xyz...
        /// </summary>
        Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    }
}
