using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace OpenSourceInitiative.LicenseApi.Caches;

/// <summary>
///     Represents a cache implementation that dynamically selects the appropriate cache mechanism
///     based on the available services. This class implements the <see cref="ILicenseCache" /> interface.
/// </summary>
/// <remarks>
///     The selection process prioritizes distributed caching mechanisms. If a distributed cache service
///     (<see cref="IDistributedCache" />) is available, it utilizes that. Otherwise, it checks for memory cache services
///     (<see cref="IMemoryCache" />). If neither is available, a fallback in-memory caching mechanism is used.
///     This class is sealed to prevent inheritance, ensuring its behavior is consistent and cannot be altered through
///     derived types.
/// </remarks>
/// <threadsafety>
///     Instances of this class delegate to the underlying cache implementation, and thread-safety depends on the
///     implementation of the selected cache mechanism.
/// </threadsafety>
internal sealed class AutoDetectCache : ILicenseCache
{
    private readonly ILicenseCache _inner;

    public AutoDetectCache(IServiceProvider sp)
    {
        var distributed = sp.GetService<IDistributedCache>();
        if (distributed != null)
        {
            _inner = new DistributedCacheAdapter(distributed);
            return;
        }

        var memory = sp.GetService<IMemoryCache>();
        if (memory != null)
        {
            _inner = new MemoryCacheAdapter(memory);
            return;
        }

        _inner = new InMemoryCacheFallback();
    }

    /// <inheritdoc />
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return _inner.GetAsync<T>(key, ct);
    }

    /// <inheritdoc />
    public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        return _inner.SetAsync(key, value, expiration, ct);
    }

    /// <inheritdoc />
    public ValueTask<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        return _inner.RemoveAsync(key, ct);
    }
}