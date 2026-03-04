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
    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable()
    {
        var cached = await cache.GetAsync<List<OsiLicense?>>(AllLicensesCacheKey).ConfigureAwait(false);
        if (cached != null)
        {
            foreach (var license in cached)
            {
                yield return license;
            }
            yield break;
        }

        var list = new List<OsiLicense?>();
        await foreach (var license in client.GetAllLicensesAsyncEnumerable().ConfigureAwait(false))
        {
            list.Add(license);
            yield return license;
        }
        await cache.SetAsync(AllLicensesCacheKey, list).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OsiLicense?> GetByOsiIdAsync(string id)
    {
        var key = OsiIdCacheKeyPrefix + id;
        var cached = await cache.GetAsync<OsiLicense?>(key).ConfigureAwait(false);
        if (cached != null) return cached;

        var license = await client.GetByOsiIdAsync(id).ConfigureAwait(false);
        if (license != null)
        {
            await cache.SetAsync(key, license).ConfigureAwait(false);
        }
        return license;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id)
    {
        var key = SpdxIdCacheKeyPrefix + id;
        var cached = await cache.GetAsync<List<OsiLicense?>>(key).ConfigureAwait(false);
        if (cached != null) return cached;

        var licenses = await client.GetBySpdxIdAsync(id).ConfigureAwait(false);
        var licenseList = licenses as List<OsiLicense?> ?? licenses.ToList();
        await cache.SetAsync(key, licenseList).ConfigureAwait(false);
        return licenseList;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name)
    {
        var key = NameCacheKeyPrefix + name;
        var cached = await cache.GetAsync<List<OsiLicense?>>(key).ConfigureAwait(false);
        if (cached != null) return cached;

        var licenses = await client.GetByNameAsync(name).ConfigureAwait(false);
        var licenseList = licenses as List<OsiLicense?> ?? licenses.ToList();
        await cache.SetAsync(key, licenseList).ConfigureAwait(false);
        return licenseList;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword)
    {
        var key = KeywordCacheKeyPrefix + OsiLicenseKeywordMapping.ToApiValue(keyword);
        var cached = await cache.GetAsync<List<OsiLicense?>>(key).ConfigureAwait(false);
        if (cached != null) return cached;

        var licenses = await client.GetByKeywordAsync(keyword).ConfigureAwait(false);
        var licenseList = licenses as List<OsiLicense?> ?? licenses.ToList();
        await cache.SetAsync(key, licenseList).ConfigureAwait(false);
        return licenseList;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward)
    {
        var key = StewardCacheKeyPrefix + steward;
        var cached = await cache.GetAsync<List<OsiLicense?>>(key).ConfigureAwait(false);
        if (cached != null) return cached;

        var licenses = await client.GetByStewardAsync(steward).ConfigureAwait(false);
        var licenseList = licenses as List<OsiLicense?> ?? licenses.ToList();
        await cache.SetAsync(key, licenseList).ConfigureAwait(false);
        return licenseList;
    }
    
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
        await client.DisposeAsync().ConfigureAwait(false);
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