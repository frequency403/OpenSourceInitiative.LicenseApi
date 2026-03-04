using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace OpenSourceInitiative.LicenseApi.Caches;

/// <summary>
/// Provides an adapter implementation for <see cref="IDistributedCache"/> to conform to the <see cref="ILicenseCache"/> interface.
/// </summary>
/// <remarks>
/// This class enables distributed caching functionality for storing and retrieving license-related data.
/// It uses JSON serialization for storing objects in the distributed cache.
/// </remarks>
internal class DistributedCacheAdapter(IDistributedCache cache) : ILicenseCache
{
    /// <inheritdoc/>
    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null 
            ? default 
            : JsonSerializer.Deserialize<T>(bytes);
    }

    /// <inheritdoc/>
    public async ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        byte[] bytes = [];
        using (var memoryStream = new MemoryStream())
        {
            await JsonSerializer.SerializeAsync(memoryStream, value, cancellationToken: ct);
            memoryStream.ToArray();
        }

        var options = new DistributedCacheEntryOptions();

        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration;

        await cache.SetAsync(key, bytes, options, ct);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(key, ct);
        return (await cache.GetAsync(key, ct)) is null;
    }
}