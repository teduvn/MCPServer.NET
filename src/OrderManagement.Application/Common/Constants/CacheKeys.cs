using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Constants
{
    /// <summary>
    /// Tập trung tất cả cache key pattern vào một chỗ.
    /// Tránh magic string rải rác, dễ refactor khi cần.
    /// </summary>
    public static class CacheKeys
    {
        // Prefix theo entity — dùng cho RemoveByPrefix khi invalidate
        private const string OrderPrefix = "orders:";
        private const string CustomerPrefix = "customers:";

        // Key cho từng query cụ thể
        public static string OrdersByCustomer(Guid customerId)
            => $"{OrderPrefix}customer:{customerId}";

        public static string OrderById(Guid orderId)
            => $"{OrderPrefix}id:{orderId}";

        public static string ActiveOrdersByCustomer(Guid customerId)
            => $"{OrderPrefix}active:customer:{customerId}";

        public static string CustomerById(Guid customerId)
            => $"{CustomerPrefix}id:{customerId}";

        // Prefix để invalidate theo nhóm
        public static string OrderPrefixForCustomer(Guid customerId)
            => $"{OrderPrefix}customer:{customerId}";
    }

}
