using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Caching
{
    public sealed class CacheSettings
    {
        public const string SectionName = "Cache";

        // TTL mặc định — override trong appsettings
        public int DefaultExpiryMinutes { get; init; } = 30;

        // Connection string đến Redis
        public string ConnectionString { get; init; } = "localhost:6379";

        public bool UseInMemory { get; init; } = false;
    }

}
