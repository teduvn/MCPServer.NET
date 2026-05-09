using Microsoft.Extensions.Caching.Memory;
using OrderManagement.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Caching
{
    /// <summary>
    /// Implementation dùng IMemoryCache — cho development/testing.
    /// KHÔNG dùng trong production multi-instance vì cache không shared giữa instances.
    /// </summary>
    public sealed class InMemoryCacheService(
        IMemoryCache memoryCache)
        : ICacheService
    {
        private readonly HashSet<string> _keys = new();
        private readonly Lock _lock = new();

        public Task<T?> GetAsync<T>(string key,
            CancellationToken ct = default) where T : class
        {
            var result = memoryCache.TryGetValue(key, out T? value)
                ? value
                : null;
            return Task.FromResult(result);
        }

        public Task SetAsync<T>(string key, T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default) where T : class
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiry ?? TimeSpan.FromMinutes(30));
            memoryCache.Set(key, value, options);
            lock (_lock) { _keys.Add(key); }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key,
            CancellationToken ct = default)
        {
            memoryCache.Remove(key);
            lock (_lock) { _keys.Remove(key); }
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix,
            CancellationToken ct = default)
        {
            List<string> toRemove;
            lock (_lock)
            {
                toRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
            }
            foreach (var key in toRemove)
            {
                memoryCache.Remove(key);
                lock (_lock) { _keys.Remove(key); }
            }
            return Task.CompletedTask;
        }
    }

}
