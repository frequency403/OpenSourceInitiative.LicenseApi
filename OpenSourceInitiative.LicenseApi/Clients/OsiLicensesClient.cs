using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET10_0_OR_GREATER
using System.Net.Http.Json;
#else
using System.Text.Json;
#endif
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Converter;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Clients;

/// <summary>
/// Default implementation of <see cref="IOsiLicensesClient"/>.
/// </summary>
/// <remarks>
/// - Uses an internal, thread-safe map to store licenses and a stable snapshot list for enumeration.
/// - Fetches the OSI catalog and extracts license text from the referenced HTML page per item.
/// - Network calls are fail-safe; on any error the current snapshot is returned (possibly empty).
/// - All async APIs have synchronous counterparts for environments where async is not desired.
/// </remarks>
public class OsiLicensesClient : IOsiLicensesClient
{
    /// <summary>
    /// The base address of the OSI API and the relative path for licenses.
    /// </summary>
    private const string ApiBase = "https://opensource.org/api/";
    private const string LicensesPath = "licenses";
    
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private volatile bool _initialized;

    // Backing storage optimized for lookups and thread-safety
    private readonly ConcurrentDictionary<string, OsiLicense> _licenses = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<OsiLicense> _snapshot = Array.Empty<OsiLicense>();

    /// <summary>
    /// Read-only, fail-safe view of the last loaded licenses snapshot.
    /// </summary>
    public IReadOnlyList<OsiLicense> Licenses
    {
        get => _snapshot;
        private set => _snapshot = value;
    }

    /// <summary>
    /// Creates a client with its own <see cref="HttpClient"/> pointing to the OSI API base URL.
    /// </summary>
    public OsiLicensesClient()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(ApiBase) };
        _ownsHttpClient = true;
    }

    /// <summary>
    /// Creates a client that uses the provided <paramref name="httpClient"/> (not disposed by this instance).
    /// </summary>
    /// <param name="httpClient">Configured HTTP client. If <see cref="HttpClient.BaseAddress"/> is null, it will be set to the OSI API base.</param>
    public OsiLicensesClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = false;
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(ApiBase);
        }
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;
        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;
            await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initGate.Release();
        }
    }

    /// <inheritdoc />
    public void Initialize()
    {
        InitializeAsync().GetAwaiter().GetResult();
    }
#if NET10_0_OR_GREATER
    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: if already populated and not cancelled, return snapshot
        if (_snapshot.Count > 0) return _snapshot;

        // Stream the licenses list and then fetch texts concurrently (bounded)
        var list = new List<OsiLicense>(capacity: 256);
        try
        {
            await foreach (var license in _httpClient.GetFromJsonAsAsyncEnumerable<OsiLicense>(
                               LicensesPath, cancellationToken))
            {
                if (license is null) continue;
                if (!TryGetLicenseKey(license, out var key)) continue;
                // Add without text first; text fetched in parallel later
                _licenses.AddOrUpdate(key, license, (_, _) => license);
            }
        }
        catch
        {
            // Ignore streaming errors; attempt JSON array fallback below
        }

        // Fallback: if streaming returned nothing, try parsing as a JSON array
        if (_licenses.IsEmpty)
        {
            try
            {
                using var stream = await _httpClient.GetStreamAsync(LicensesPath, cancellationToken).ConfigureAwait(false);
                var arr = await System.Text.Json.JsonSerializer.DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (arr is not null)
                {
                    foreach (var lic in arr)
                    {
                        if (lic is null) continue;
                        if (!TryGetLicenseKey(lic, out var k)) continue;
                        _licenses.AddOrUpdate(k, lic, (_, _) => lic);
                    }
                }
            }
            catch
            {
                // still fail-safe; leave dictionary empty
            }
        }

        // Bounded parallelism for fetching license texts
        var throttler = new SemaphoreSlim(Math.Max(2, Environment.ProcessorCount));
        var tasks = new List<Task>();
        foreach (var kvp in _licenses)
        {
            var license = kvp.Value;
            if (!string.IsNullOrWhiteSpace(license.LicenseText)) continue;
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Individual call is fail-safe
                    var text = await _httpClient.GetLicenseTextAsync(license, cancellationToken).ConfigureAwait(false);
                    license.LicenseText = text;
                }
                catch
                {
                    // keep going; leave LicenseText as null on failure
                }
                finally
                {
                    throttler.Release();
                }
            }, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            // Ignore aggregate exceptions from cancelled tasks; fail-safe behavior
        }

        list.AddRange(_licenses.Values);
        // Sort for deterministic order (by SPDX id if available, else by name)
        list.Sort(static (a, b) => string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));
        Licenses = list;
        return _snapshot;
    }
#else
    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(CancellationToken cancellationToken = default)
    {
        if (_snapshot.Count > 0) return _snapshot;
        OsiLicense[]? licensesWithoutText;
        try
        {
            using var stream = await _httpClient.GetStreamAsync(LicensesPath).ConfigureAwait(false);
            licensesWithoutText = await JsonSerializer.DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return _snapshot; // fail-safe
        }

        if (licensesWithoutText is null || licensesWithoutText.Length == 0)
            return _snapshot;

        foreach (var license in licensesWithoutText)
        {
            if (license is null) continue;
            if (!TryGetLicenseKey(license, out var key)) continue;
            _licenses.AddOrUpdate(key, license, (_, _) => license);
        }

        // Bounded parallelism
        var throttler = new SemaphoreSlim(Math.Max(2, Environment.ProcessorCount));
        var tasks = new List<Task>();
        foreach (var kvp in _licenses)
        {
            var license = kvp.Value;
            if (!string.IsNullOrWhiteSpace(license.LicenseText)) continue;
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var text = await _httpClient.GetLicenseTextAsync(license, cancellationToken).ConfigureAwait(false);
                    license.LicenseText = text;
                }
                catch
                {
                    // ignore per-item failure
                }
                finally
                {
                    throttler.Release();
                }
            }, cancellationToken));
        }

        try { await Task.WhenAll(tasks).ConfigureAwait(false); }
        catch { /* ignore */ }

        var list = _licenses.Values.ToList();
        list.Sort(static (a, b) => string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));
        Licenses = list;
        return _snapshot;
    }
#endif

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetAllLicenses()
        => GetAllLicensesAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<OsiLicense>();
        await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
        var q = query.Trim();
        return _snapshot.Where(l =>
                (!string.IsNullOrEmpty(l.Name) && l.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(l.Id) && l.Id.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> Search(string query)
        => SearchAsync(query).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<OsiLicense?> GetBySpdxAsync(string spdxId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spdxId)) return null;
        await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
        // Try fast path via dictionary (keys prefer SPDX when available)
        if (_licenses.TryGetValue(spdxId, out var lic)) return lic;
        // Fallback scan
        return _snapshot.FirstOrDefault(l => string.Equals(l.SpdxId, spdxId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public OsiLicense? GetBySpdx(string spdxId)
        => GetBySpdxAsync(spdxId).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByNameAsync(string name, CancellationToken cancellationToken = default)
        => FetchFilteredAsync("name", name, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(string keyword, CancellationToken cancellationToken = default)
        => FetchFilteredAsync("keyword", keyword, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(OsiLicenseKeyword keyword, CancellationToken cancellationToken = default)
        => FetchFilteredAsync("keyword", OsiLicenseKeywordMapping.ToApiValue(keyword), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByStewardAsync(string steward, CancellationToken cancellationToken = default)
        => FetchFilteredAsync("steward", steward, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesBySpdxPatternAsync(string spdxPattern, CancellationToken cancellationToken = default)
        => FetchFilteredAsync("spdx", spdxPattern, cancellationToken);

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesByName(string name)
        => GetLicensesByNameAsync(name).GetAwaiter().GetResult();

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesByKeyword(string keyword)
        => GetLicensesByKeywordAsync(keyword).GetAwaiter().GetResult();

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesByKeyword(OsiLicenseKeyword keyword)
        => GetLicensesByKeywordAsync(keyword).GetAwaiter().GetResult();

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesBySteward(string steward)
        => GetLicensesByStewardAsync(steward).GetAwaiter().GetResult();

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesBySpdxPattern(string spdxPattern)
        => GetLicensesBySpdxPatternAsync(spdxPattern).GetAwaiter().GetResult();

    private async Task<IReadOnlyList<OsiLicense>> FetchFilteredAsync(string paramName, string paramValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paramValue)) return Array.Empty<OsiLicense>();
        string encoded = Uri.EscapeDataString(paramValue);
        if (string.Equals(paramName, "spdx", StringComparison.OrdinalIgnoreCase))
        {
            // Preserve '*' wildcard per API spec
            encoded = encoded.Replace("%2A", "*");
        }
        var request = $"{LicensesPath}?{paramName}={encoded}";
        OsiLicense[]? items;
        try
        {
            using var stream = await _httpClient.GetStreamAsync(request
#if NET10_0_OR_GREATER
                , cancellationToken
#endif
            ).ConfigureAwait(false);
#if NET10_0_OR_GREATER
            items = await System.Text.Json.JsonSerializer.DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
#else
            items = await JsonSerializer.DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
#endif
        }
        catch
        {
            return Array.Empty<OsiLicense>();
        }

        if (items is null || items.Length == 0)
            return Array.Empty<OsiLicense>();

        // Map into a temp list and enrich with license text
        var list = new List<OsiLicense>(items.Length);
        foreach (var license in items)
        {
            if (license is null) continue;
            if (TryGetLicenseKey(license, out var key))
            {
                _licenses.AddOrUpdate(key!, license, (_, _) => license);
            }
            list.Add(license);
        }

        // Enrich with license text in parallel (bounded)
        var throttler = new SemaphoreSlim(Math.Max(2, Environment.ProcessorCount));
        var tasks = new List<Task>();
        foreach (var lic in list)
        {
            if (!string.IsNullOrWhiteSpace(lic.LicenseText)) continue;
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var text = await _httpClient.GetLicenseTextAsync(lic, cancellationToken).ConfigureAwait(false);
                    lic.LicenseText = text;
                }
                catch { /* fail-safe per item */ }
                finally { throttler.Release(); }
            }, cancellationToken));
        }

        try { await Task.WhenAll(tasks).ConfigureAwait(false); }
        catch { /* ignore */ }

        // Deterministic order
        list.Sort(static (a, b) => string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    private static bool TryGetLicenseKey(OsiLicense license, out string? key)
    {
        key = license.SpdxId;
        if (!string.IsNullOrWhiteSpace(key)) return true;
        key = license.Name;
        return !string.IsNullOrWhiteSpace(key);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
        _initGate.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        try
        {
            _initGate.Dispose();
        }
        finally
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
            await Task.CompletedTask;
        }
    }

    
}