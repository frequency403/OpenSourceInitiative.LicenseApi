using Microsoft.Extensions.Caching.Memory;

namespace OpenSourceInitiative.LicenseApi.Caches;

/// <summary>
/// Provides an implementation of the <see cref="ILicenseCache"/> interface using
/// <see cref="IMemoryCache"/> to store and retrieve cached data in memory.
/// </summary>
internal class MemoryCacheAdapter(IMemoryCache cache) : ILicenseCache
{
    /// <inheritdoc/>
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        cache.TryGetValue(key, out T? value);
#if !NETSTANDARD2_0
        return ValueTask.FromResult(value);
#else
        return new ValueTask<T?>(value);
#endif
    }

    /// <inheritdoc/>
    public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration;

        cache.Set(key, value, options);
#if !NETSTANDARD2_0
        return ValueTask.CompletedTask;
#else
        return new ValueTask();
#endif
    }

    /// <inheritdoc/>
    public ValueTask<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
#if !NETSTANDARD2_0
        return ValueTask.FromResult(cache.Get(key) is null);
#else
        return new ValueTask<bool>(cache.Get(key) is null);
#endif
    }
}