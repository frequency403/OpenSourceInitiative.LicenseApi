using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Clients;

public class OsiCachingClient([FromKeyedServices("OsiNonCachingClient")] IOsiClient client) : IOsiClient
{
    private readonly ConcurrentDictionary<string, Task<OsiLicense?>> _osiIdCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Task<IEnumerable<OsiLicense?>>> _queryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _streamLock = new(1, 1);
    private List<OsiLicense?>? _allLicensesCache;

    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable()
    {
        if (_allLicensesCache != null)
        {
            foreach (var license in _allLicensesCache) yield return license;
            yield break;
        }

        await _streamLock.WaitAsync();
        try
        {
            if (_allLicensesCache != null)
            {
                foreach (var license in _allLicensesCache) yield return license;
                yield break;
            }

            var list = new List<OsiLicense?>();
            await foreach (var license in client.GetAllLicensesAsyncEnumerable())
            {
                list.Add(license);
                yield return license;
            }
            _allLicensesCache = list;
        }
        finally
        {
            _streamLock.Release();
        }
    }

    public Task<OsiLicense?> GetByOsiIdAsync(string id)
    {
        return _osiIdCache.GetOrAdd(id, client.GetByOsiIdAsync);
    }

    public Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id)
    {
        return _queryCache.GetOrAdd($"spdx:{id}", _ => client.GetBySpdxIdAsync(id));
    }

    public Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name)
    {
        return _queryCache.GetOrAdd($"name:{name}", _ => client.GetByNameAsync(name));
    }

    public Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword)
    {
        return _queryCache.GetOrAdd($"keyword:{keyword}", _ => client.GetByKeywordAsync(keyword));
    }

    public Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward)
    {
        return _queryCache.GetOrAdd($"steward:{steward}", _ => client.GetByStewardAsync(steward));
    }

    public void Dispose()
    {
        _streamLock.Dispose();
        client.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _streamLock.Dispose();
        await client.DisposeAsync();
    }
}