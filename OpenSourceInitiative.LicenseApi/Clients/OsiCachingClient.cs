using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Caches;
using OpenSourceInitiative.LicenseApi.Converter;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Clients;

internal sealed class OsiCachingClient([FromKeyedServices(ServiceCollectionExtensions.OsiClientNonCachingName)] IOsiClient client, ILicenseCache cache) : IOsiClient
{
    private const string AllLicensesCacheKey = "all_licenses";
    private const string OsiIdCacheKeyPrefix = "osi_id_";
    private const string SpdxIdCacheKeyPrefix = "spdx_id_";
    private const string NameCacheKeyPrefix = "name_";
    private const string KeywordCacheKeyPrefix = "keyword_";
    private const string StewardCacheKeyPrefix = "steward_";

    /// <inheritdoc />
    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable([EnumeratorCancellation] CancellationToken token = default)
    {
        if (await GetLicenseFromCacheByKeyAsync(AllLicensesCacheKey, token) is {  } cached)
        {
            foreach (var license in cached)
            {
                yield return license;
            }
            yield break;
        }

        var list = new List<OsiLicense?>();
        await foreach (var license in client.GetAllLicensesAsyncEnumerable(token).ConfigureAwait(false))
        {
            list.Add(license);
            yield return license;
        }
        await cache.SetAsync(AllLicensesCacheKey, list, ct: token);
    }

    /// <inheritdoc />
    public async Task<OsiLicense?> GetByOsiIdAsync(string id, CancellationToken token = default)
    {
        var key = OsiIdCacheKeyPrefix + id;
        var cached = await cache.GetAsync<OsiLicense?>(key, token);
        if (cached != null) return cached;

        var license = await client.GetByOsiIdAsync(id, token);
        if (license != null)
        {
            await cache.SetAsync(key, license, ct: token);
        }
        return license;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id, CancellationToken token = default)
    {
        var key = SpdxIdCacheKeyPrefix + id;
        if (await GetLicenseFromCacheByKeyAsync(key, token) is {  } cached)
            return cached;

        if (await client.GetBySpdxIdAsync(id, token) is not { } licenses) 
            return [];
        var licenseList = licenses as List<OsiLicense?> ?? licenses.ToList();
        await cache.SetAsync(key, licenseList, ct: token);
        return licenseList;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken token = default)
    {
        var key = NameCacheKeyPrefix + name;
        if (await GetLicenseFromCacheByKeyAsync(key, token) is {  } cached)
            return cached;

        if (await client.GetByNameAsync(name, token) is not { } licenses) 
            return [];
        
        var licenseList = licenses as List<OsiLicense?> ?? licenses.ToList();
        await cache.SetAsync(key, licenseList, ct: token);
        return licenseList;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword, CancellationToken token = default)
    {
        var key = KeywordCacheKeyPrefix + OsiLicenseKeywordMapping.ToApiValue(keyword);
        if (await GetLicenseFromCacheByKeyAsync(key, token) is {  } cached)
            return cached;
        
        if(await client.GetByKeywordAsync(keyword, token) is not { } licenses) 
            return [];
        
        var licenseList = licenses as List<OsiLicense?> ?? licenses.ToList();
        await cache.SetAsync(key, licenseList, ct: token);
        return licenseList;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward, CancellationToken token = default)
    {
        var key = StewardCacheKeyPrefix + steward;
        if (await GetLicenseFromCacheByKeyAsync(key, token) is {  } cached)
            return cached;
        if (await client.GetByStewardAsync(steward, token) is not { } licenses) return [];
        
        var licenseList = licenses as List<OsiLicense?> ?? licenses.ToList();
        await cache.SetAsync(key, licenseList, ct: token);
        return licenseList;
    }
    
    private ValueTask<List<OsiLicense?>?> GetLicenseFromCacheByKeyAsync(string key, CancellationToken token)
        => cache.GetAsync<List<OsiLicense?>>(key, token);
    
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            client.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsyncCore()
    {
        await client.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    ~OsiCachingClient()
    {
        Dispose(false);
    }
}