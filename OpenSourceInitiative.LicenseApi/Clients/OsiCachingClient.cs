using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Clients;

public class OsiCachingClient([FromKeyedServices("OsiNonCachingClient")] IOsiClient client) : IOsiClient
{
    private readonly ConcurrentDictionary<string, OsiLicense?> _osiIdCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IEnumerable<OsiLicense?>> _queryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<OsiLicense?> _licenses = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _allFetched;

    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable()
    {
        if (_allFetched)
        {
            foreach (var license in _licenses) yield return license;
            yield break;
        }

        await _lock.WaitAsync();
        try
        {
            if (_allFetched)
            {
                foreach (var license in _licenses) yield return license;
                yield break;
            }

            await foreach (var license in client.GetAllLicensesAsyncEnumerable())
            {
                if (license != null)
                {
                    UpdateCachesInternal(license);
                }
                yield return license;
            }
            _allFetched = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void UpdateCachesInternal(OsiLicense license)
    {
        if (_osiIdCache.TryAdd(license.Id, license))
        {
            _licenses.Add(license);
        }
    }

    public async Task<OsiLicense?> GetByOsiIdAsync(string id)
    {
        if (_osiIdCache.TryGetValue(id, out var license)) return license;

        await _lock.WaitAsync();
        try
        {
            if (_osiIdCache.TryGetValue(id, out license)) return license;

            license = await client.GetByOsiIdAsync(id);
            if (license != null)
            {
                UpdateCachesInternal(license);
            }
            return license;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IEnumerable<OsiLicense?>> GetOrFetchQueryAsync(string cacheKey, Func<Task<IEnumerable<OsiLicense?>>> fetcher)
    {
        if (_queryCache.TryGetValue(cacheKey, out var results)) return results;

        await _lock.WaitAsync();
        try
        {
            if (_queryCache.TryGetValue(cacheKey, out results)) return results;

            results = (await fetcher()).ToList();
            _queryCache[cacheKey] = results;
            foreach (var license in results)
            {
                if (license != null) UpdateCachesInternal(license);
            }
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id)
    {
        return GetOrFetchQueryAsync($"spdx:{id}", () => client.GetBySpdxIdAsync(id));
    }

    public Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name)
    {
        return GetOrFetchQueryAsync($"name:{name}", () => client.GetByNameAsync(name));
    }

    public Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword)
    {
        return GetOrFetchQueryAsync($"keyword:{keyword}", () => client.GetByKeywordAsync(keyword));
    }

    public Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward)
    {
        return GetOrFetchQueryAsync($"steward:{steward}", () => client.GetByStewardAsync(steward));
    }

    public void Dispose()
    {
        _lock.Dispose();
        client.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _lock.Dispose();
        await client.DisposeAsync();
    }
}