using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET10_0_OR_GREATER
using System.Net.Http.Json;
#else
using System.Text.Json;
#endif
using OpenSourceInitiative.LicenseApi.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

    private readonly ILogger<OsiLicensesClient> _logger;
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
    /// <param name="logger">Optional logger instance.</param>   
    public OsiLicensesClient(ILogger<OsiLicensesClient>? logger = null)
    {
        _logger = logger ?? NullLogger<OsiLicensesClient>.Instance;
        _httpClient = new HttpClient { BaseAddress = new Uri(ApiBase) };
        EnsureDefaultHeaders(_httpClient);
        _ownsHttpClient = true;
    }

    /// <summary>
    /// Creates a client that uses the provided <paramref name="httpClient"/> (not disposed by this instance).
    /// </summary>
    /// <param name="httpClient">Configured HTTP client. If <see cref="HttpClient.BaseAddress"/> is null, it will be set to the OSI API base.</param>
    /// <param name="logger">Optional logger instance.</param> 
    public OsiLicensesClient(HttpClient httpClient, ILogger<OsiLicensesClient>? logger = null)
    {
        _logger = logger ?? NullLogger<OsiLicensesClient>.Instance;
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = false;
        _httpClient.BaseAddress ??= new Uri(ApiBase);
        EnsureDefaultHeaders(_httpClient);
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        _logger.LogDebug("Acquiring initialization lock");
        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                _logger.LogDebug("Already initialized, skipping");
                return;
            }

            _logger.LogInformation("Initializing OsiLicensesClient");
            await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
            _logger.LogInformation("OsiLicensesClient initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OsiLicensesClient");
            throw;
        }
        finally
        {
            _initGate.Release();
        }
    }

    /// <inheritdoc />
    public void Initialize()
    {
        _logger.LogDebug("Starting synchronous initialization");
        InitializeAsync().GetAwaiter().GetResult();
    }

#if NET10_0_OR_GREATER
    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: if already populated and not cancelled, return snapshot
        if (_snapshot.Count > 0)
        {
            _logger.LogDebug("Returning cached snapshot of {Count} licenses", _snapshot.Count);
            return _snapshot;
        }

        _logger.LogInformation("Fetching all licenses from OSI API");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Stream the licenses list and then fetch texts concurrently (bounded)
        var list = new List<OsiLicense>(capacity: 256);
        try
        {
            _logger.LogDebug("Starting streaming deserialization from {Path}", LicensesPath);
            await foreach (var license in _httpClient.GetFromJsonAsAsyncEnumerable<OsiLicense>(
                               LicensesPath, cancellationToken))
            {
                if (license is null) continue;
                if (!TryGetLicenseKey(license, out var key)) continue;
                // Add without text first; text fetched in parallel later
                _licenses.AddOrUpdate(key, license, (_, _) => license);
            }

            _logger.LogDebug("Streaming deserialization completed, loaded {Count} licenses", _licenses.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Streaming deserialization failed, falling back to array deserialization");
            // Ignore streaming errors; attempt JSON array fallback below
        }

        // Fallback: if streaming returned nothing, try parsing as a JSON array
        if (_licenses.IsEmpty)
        {
            _logger.LogDebug("Attempting fallback array deserialization");
            try
            {
                using var stream =
                    await _httpClient.GetStreamAsync(LicensesPath, cancellationToken).ConfigureAwait(false);
                var arr = await System.Text.Json.JsonSerializer
                    .DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (arr is not null)
                {
                    _logger.LogDebug("Fallback deserialization successful, processing {Count} licenses", arr.Length);
                    foreach (var lic in arr)
                    {
                        if (lic is null) continue;
                        if (!TryGetLicenseKey(lic, out var k)) continue;
                        _licenses.AddOrUpdate(k, lic, (_, _) => lic);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback deserialization failed");
                // still fail-safe; leave dictionary empty
            }
        }

        // Bounded parallelism for fetching license texts
        var maxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount);
        _logger.LogDebug("Fetching license texts with max parallelism of {MaxParallelism}", maxDegreeOfParallelism);
        var throttler = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new List<Task>();
        var textFetchCount = 0;
        foreach (var kvp in _licenses)
        {
            var license = kvp.Value;
            if (!string.IsNullOrWhiteSpace(license.LicenseText)) continue;
            textFetchCount++;
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Individual call is fail-safe
                    var text = await _httpClient.GetLicenseTextAsync(license, cancellationToken).ConfigureAwait(false);
                    license.LicenseText = text;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch license text for {LicenseName} (SPDX: {SpdxId})",
                        license.Name, license.SpdxId);
                    // keep going; leave LicenseText as null on failure
                }
                finally
                {
                    throttler.Release();
                }
            }, cancellationToken));
        }

        _logger.LogDebug("Initiated {Count} license text fetch operations", textFetchCount);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
            _logger.LogDebug("All license text fetch operations completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Some license text fetch operations failed");
            // Ignore aggregate exceptions from cancelled tasks; fail-safe behavior
        }

        list.AddRange(_licenses.Values);
        // Sort for deterministic order (by SPDX id if available, else by name)
        list.Sort(static (a, b) =>
            string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));
        Licenses = list;

        sw.Stop();
        _logger.LogInformation("Successfully loaded {Count} licenses in {ElapsedMs} ms", _snapshot.Count,
            sw.ElapsedMilliseconds);
        return _snapshot;
    }
#else
    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(CancellationToken cancellationToken = default)
    {
        if (_snapshot.Count > 0)
        {
            _logger.LogDebug("Returning cached snapshot of {Count} licenses", _snapshot.Count);
            return _snapshot;
        }
        
        _logger.LogInformation("Fetching all licenses from OSI API");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        OsiLicense[]? licensesWithoutText;
        try
        {
            _logger.LogDebug("Fetching licenses from {Path}", LicensesPath);
            using var stream = await _httpClient.GetStreamAsync(LicensesPath).ConfigureAwait(false);
            licensesWithoutText =
 await JsonSerializer.DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Deserialized {Count} licenses", licensesWithoutText?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch or deserialize licenses from API");
            return _snapshot; // fail-safe
        }

        if (licensesWithoutText is null || licensesWithoutText.Length == 0)
        {
            _logger.LogWarning("No licenses returned from API");
            return _snapshot;
        }

        foreach (var license in licensesWithoutText)
        {
            if (!TryGetLicenseKey(license, out var key)) continue;
            _licenses.AddOrUpdate(key, license, (_, _) => license);
        }

        // Bounded parallelism
        var maxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount);
        _logger.LogDebug("Fetching license texts with max parallelism of {MaxParallelism}", maxDegreeOfParallelism);
        using var throttler = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new List<Task>();
        var textFetchCount = 0;
        foreach (var kvp in _licenses)
        {
            var license = kvp.Value;
            if (!string.IsNullOrWhiteSpace(license.LicenseText)) continue;
            textFetchCount++;
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var text = await _httpClient.GetLicenseTextAsync(license, cancellationToken).ConfigureAwait(false);
                    license.LicenseText = text;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch license text for {LicenseName} (SPDX: {SpdxId})", license.Name, license.SpdxId);
                    // ignore per-item failure
                }
                finally
                {
                    throttler.Release();
                }
            }, cancellationToken));
        }

        _logger.LogDebug("Initiated {Count} license text fetch operations", textFetchCount);

        try 
        { 
            await Task.WhenAll(tasks).ConfigureAwait(false);
            _logger.LogDebug("All license text fetch operations completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Some license text fetch operations failed");
            /* ignore */ 
        }

        var list = _licenses.Values.ToList();
        list.Sort(static (a, b) => string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));
        Licenses = list;
        
        sw.Stop();
        _logger.LogInformation("Successfully loaded {Count} licenses in {ElapsedMs} ms", _snapshot.Count, sw.ElapsedMilliseconds);
        return _snapshot;
    }
#endif

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetAllLicenses()
        => GetAllLicensesAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> SearchAsync(string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("Search called with empty query, returning empty result");
            return Array.Empty<OsiLicense>();
        }

        _logger.LogDebug("Searching for licenses matching query: '{Query}'", query);
        await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
        var q = query.Trim();
        var results = _snapshot.Where(l =>
                (!string.IsNullOrEmpty(l.Name) && l.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(l.Id) && l.Id.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        _logger.LogInformation("Search for '{Query}' returned {Count} result(s)", query, results.Length);
        return results;
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> Search(string query)
        => SearchAsync(query).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<OsiLicense?> GetBySpdxAsync(string spdxId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spdxId))
        {
            _logger.LogDebug("GetBySpdx called with empty SPDX ID");
            return null;
        }

        _logger.LogDebug("Looking up license by SPDX ID: '{SpdxId}'", spdxId);
        await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
        // Try fast path via dictionary (keys prefer SPDX when available)
        if (_licenses.TryGetValue(spdxId, out var lic))
        {
            _logger.LogDebug("Found license '{LicenseName}' via dictionary lookup", lic.Name);
            return lic;
        }

        // Fallback scan
        var result = _snapshot.FirstOrDefault(l => string.Equals(l.SpdxId, spdxId, StringComparison.OrdinalIgnoreCase));
        if (result != null)
        {
            _logger.LogDebug("Found license '{LicenseName}' via fallback scan", result.Name);
        }
        else
        {
            _logger.LogInformation("License with SPDX ID '{SpdxId}' not found", spdxId);
        }

        return result;
    }

    /// <inheritdoc />
    public OsiLicense? GetBySpdx(string spdxId)
        => GetBySpdxAsync(spdxId).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByNameAsync(string name,
        CancellationToken cancellationToken = default)
        => FetchFilteredAsync("name", name, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(string keyword,
        CancellationToken cancellationToken = default)
        => FetchFilteredAsync("keyword", keyword, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(OsiLicenseKeyword keyword,
        CancellationToken cancellationToken = default)
        => FetchFilteredAsync("keyword", OsiLicenseKeywordMapping.ToApiValue(keyword), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByStewardAsync(string steward,
        CancellationToken cancellationToken = default)
        => FetchFilteredAsync("steward", steward, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesBySpdxPatternAsync(string spdxPattern,
        CancellationToken cancellationToken = default)
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

    private async Task<IReadOnlyList<OsiLicense>> FetchFilteredAsync(string paramName, string paramValue,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paramValue))
        {
            _logger.LogDebug("FetchFiltered called with empty value for parameter '{ParamName}'", paramName);
            return Array.Empty<OsiLicense>();
        }

        _logger.LogDebug("Fetching licenses filtered by {ParamName}='{ParamValue}'", paramName, paramValue);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        string encoded = Uri.EscapeDataString(paramValue);
        if (string.Equals(paramName, "spdx", StringComparison.OrdinalIgnoreCase))
        {
            // Preserve '*' wildcard per API spec
            encoded = encoded.Replace("%2A", "*");
        }

        var request = $"{LicensesPath}?{paramName}={encoded}";
        _logger.LogDebug("Request URL: {RequestUrl}", request);

        OsiLicense[]? items;
        try
        {
            using var stream = await _httpClient.GetStreamAsync(request
#if NET10_0_OR_GREATER
                , cancellationToken
#endif
            ).ConfigureAwait(false);
#if NET10_0_OR_GREATER
            items = await System.Text.Json.JsonSerializer
                .DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
#else
            items =
 await JsonSerializer.DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
#endif
            _logger.LogDebug("Deserialized {Count} filtered licenses", items?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch filtered licenses by {ParamName}='{ParamValue}'", paramName,
                paramValue);
            return Array.Empty<OsiLicense>();
        }

        if (items is null || items.Length == 0)
        {
            _logger.LogInformation("No licenses found for {ParamName}='{ParamValue}'", paramName, paramValue);
            return Array.Empty<OsiLicense>();
        }

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
        var maxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount);
        _logger.LogDebug("Enriching {Count} filtered licenses with text (max parallelism: {MaxParallelism})",
            list.Count, maxDegreeOfParallelism);
        using var throttler = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new List<Task>();
        var textFetchCount = 0;
        foreach (var lic in list.Where(lic => string.IsNullOrWhiteSpace(lic.LicenseText)))
        {
            textFetchCount++;
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var text = await _httpClient.GetLicenseTextAsync(lic, cancellationToken).ConfigureAwait(false);
                    lic.LicenseText = text;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch license text for {LicenseName} (SPDX: {SpdxId})", lic.Name,
                        lic.SpdxId);
                    /* fail-safe per item */
                }
                finally
                {
                    throttler.Release();
                }
            }, cancellationToken));
        }

        _logger.LogDebug("Initiated {Count} license text fetch operations for filtered results", textFetchCount);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
            _logger.LogDebug("All license text fetch operations completed for filtered results");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Some license text fetch operations failed for filtered results");
            /* ignore */
        }

        // Deterministic order
        list.Sort(static (a, b) =>
            string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));

        sw.Stop();
        _logger.LogInformation(
            "Fetched and enriched {Count} licenses filtered by {ParamName}='{ParamValue}' in {ElapsedMs} ms",
            list.Count, paramName, paramValue, sw.ElapsedMilliseconds);

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
        _logger.LogDebug("Disposing OsiLicensesClient");
        GC.SuppressFinalize(this);
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
            _logger.LogDebug("Disposed owned HttpClient");
        }

        _initGate.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing OsiLicensesClient asynchronously");
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
                _logger.LogDebug("Disposed owned HttpClient");
            }

            await Task.CompletedTask;
        }
    }

    private static void EnsureDefaultHeaders(HttpClient client)
    {
        // Accept JSON by default
        if (client.DefaultRequestHeaders.Accept.Count == 0)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        if (client.DefaultRequestHeaders.UserAgent == null || client.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("OpenSourceInitiative-LicenseApi-Client", version));
        }
    }
}