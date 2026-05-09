using OrderManagement.Application.Common.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OrderManagement.Infrastructure.Caching
{
    public sealed class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly IServer _server;
        private readonly TimeSpan _defaultExpiry;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IConnectionMultiplexer connection,
            CacheSettings settings)
        {
            _db = connection.GetDatabase();
            // GetServer cần endpoint — dùng endpoint đầu tiên
            _server = connection.GetServer(connection.GetEndPoints().First());
            _defaultExpiry = TimeSpan.FromMinutes(settings.DefaultExpiryMinutes);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<T?> GetAsync<T>(string key,
            CancellationToken ct = default) where T : class
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue) return null;  // Cache miss

            return JsonSerializer.Deserialize<T>(value!.ToString(), _jsonOptions);
        }

        public async Task SetAsync<T>(string key, T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default) where T : class
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            await _db.StringSetAsync(key, serialized, expiry ?? _defaultExpiry);
        }

        public async Task RemoveAsync(string key,
            CancellationToken ct = default)
        {
            await _db.KeyDeleteAsync(key);
            // Không throw nếu key không tồn tại — idempotent
        }

        public async Task RemoveByPrefixAsync(string prefix,
            CancellationToken ct = default)
        {
            // KEYS command không dùng được trong cluster — dùng SCAN
            var keys = _server.KeysAsync(pattern: $"{prefix}*");
            await foreach (var key in keys)
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }

}
