
namespace OpenSourceInitiative.LicenseApi.Caches;

/// <summary>
/// Defines a contract for a caching mechanism for storing and retrieving license-related data.
/// </summary>
internal interface ILicenseCache
{
    /// <summary>
    /// Retrieves a value from the cache corresponding to the specified key if it exists.
    /// Returns null if the key is not found in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to be retrieved from the cache.</typeparam>
    /// <param name="key">The unique key identifying the cached item.</param>
    /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the cached value if it exists; otherwise, null.</returns>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// Asynchronously sets a key-value pair in the cache with an optional expiration time.
    /// <typeparam name="T">The type of the value to be stored in the cache.</typeparam>
    /// <param name="key">The unique cache key associated with the value.</param>
    /// <param name="value">The value to be stored in the cache.</param>
    /// <param name="expiration">
    /// An optional expiration time for the cached value. If specified, the value will expire after the given time.
    /// If not provided, the value will persist indefinitely.
    /// </param>
    /// <param name="ct">
    /// A cancellation token to observe while waiting for the task to complete.
    /// Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    /// <summary>
    /// Removes an item from the cache based on the specified key.
    /// </summary>
    /// <param name="key">The key of the item to be removed from the cache.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing a boolean value indicating whether the removal was successful.</returns>
    ValueTask<bool> RemoveAsync(string key, CancellationToken ct = default);
    
}