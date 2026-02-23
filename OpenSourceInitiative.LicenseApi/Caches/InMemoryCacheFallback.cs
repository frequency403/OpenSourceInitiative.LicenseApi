using System.Collections.Concurrent;

namespace OpenSourceInitiative.LicenseApi.Caches;

/// <summary>
/// Provides an in-memory caching implementation of the <see cref="ILicenseCache"/> interface
/// for storing and retrieving license-related data.
/// </summary>
/// <remarks>
/// This class uses a thread-safe <see cref="ConcurrentDictionary{TKey, TValue}"/> to store
/// cached items and their optional expiration times. Items that have expired are automatically
/// removed from the cache when accessed.
/// </remarks>
/// <threadsafety>
/// This implementation is thread-safe.
/// </threadsafety>
internal class InMemoryCacheFallback : ILicenseCache
{
    private readonly ConcurrentDictionary<string, (object value, DateTimeOffset? expires)> _cache = new();

    /// <inheritdoc/>
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.expires is null || entry.expires > DateTimeOffset.UtcNow)
#if !NETSTANDARD2_0
                return ValueTask.FromResult((T?)entry.value);
#else
            return new ValueTask<T?>((T?)entry.value);
#endif

            _cache.TryRemove(key, out _);
        }

#if !NETSTANDARD2_0
        return ValueTask.FromResult(default(T?));
#else
        return new ValueTask<T?>();
#endif
    }

    /// <inheritdoc/>
    public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        DateTimeOffset? expires = expiration.HasValue
            ? DateTimeOffset.UtcNow.Add(expiration.Value)
            : null;

        _cache[key] = (value!, expires);
#if !NETSTANDARD2_0
        return ValueTask.CompletedTask;
#else
        return new ValueTask();
#endif
    }

    /// <inheritdoc/>
    public ValueTask<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        var result = _cache.TryRemove(key, out _);
#if !NETSTANDARD2_0
        return ValueTask.FromResult(result);
#else
        return new ValueTask<bool>(result);
#endif
    }
}