using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSourceInitiative.LicenseApi.Converter;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Log;
using OpenSourceInitiative.LicenseApi.Models;
#if NET10_0_OR_GREATER
using System.Net.Http.Json;

#else
#endif

namespace OpenSourceInitiative.LicenseApi.Clients;

/// <summary>
///     Default implementation of <see cref="IOsiLicensesClient" />.
/// </summary>
/// <remarks>
///     - Uses an internal, thread-safe map to store licenses and a stable snapshot list for enumeration.
///     - Fetches the OSI catalog and extracts license text from the referenced HTML page per item.
///     - Network calls are fail-safe; on any error the current snapshot is returned (possibly empty).
///     - All async APIs have synchronous counterparts for environments where async is not desired.
/// </remarks>
public class OsiLicensesClient : IOsiLicensesClient
{
    /// <summary>
    ///     The base address of the OSI API and the relative path for licenses.
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

    /// <summary>
    ///     Read-only, fail-safe view of the last loaded licenses snapshot.
    /// </summary>
    public IReadOnlyList<OsiLicense> Licenses { get; private set; } = [];

    /// <summary>
    ///     Creates a client with its own <see cref="HttpClient" /> pointing to the OSI API base URL.
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
    ///     Creates a client that uses the provided <paramref name="httpClient" /> (not disposed by this instance).
    /// </summary>
    /// <param name="httpClient">
    ///     Configured HTTP client. If <see cref="HttpClient.BaseAddress" /> is null, it will be set to
    ///     the OSI API base.
    /// </param>
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

        LoggerMethods.LogAcquiringInitializationLock(_logger);
        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                LoggerMethods.LogAlreadyInitializedSkipping(_logger);
                return;
            }

            LoggerMethods.LogInitializingOsilicensesclient(_logger);
            await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
            LoggerMethods.LogOsilicensesclientInitializationCompletedSuccessfully(_logger);
        }
        catch (Exception)
        {
            LoggerMethods.LogFailedToInitializeOsilicensesclient(_logger);
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
        LoggerMethods.LogStartingSynchronousInitialization(_logger);
        InitializeAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: if already populated and not cancelled, return snapshot
        if (Licenses.Count > 0)
        {
            LoggerMethods.LogReturningCachedSnapshotOfCountLicenses(_logger, Licenses.Count);
            return Licenses;
        }

        LoggerMethods.LogFetchingAllLicensesFromOsiApi(_logger);
        var sw = Stopwatch.StartNew();

        // Stream the licenses list and then fetch texts concurrently (bounded)
        var list = new List<OsiLicense>(256);
        try
        {
            LoggerMethods.LogStartingStreamingDeserializationFromPath(_logger, LicensesPath);
#if NET10_0_OR_GREATER
            await foreach (var license in _httpClient.GetFromJsonAsAsyncEnumerable<OsiLicense>(
                               LicensesPath, cancellationToken))
            {
                if (license is null) continue;
#else
            var requestStream = await _httpClient.GetStreamAsync(LicensesPath)
                .ConfigureAwait(false);
            var deserializedLicenses =
                await JsonSerializer.DeserializeAsync<OsiLicense[]>(requestStream,
                    cancellationToken: cancellationToken);
            if(deserializedLicenses is null)
                throw new System.Runtime.Serialization.SerializationException("Failed to deserialize licenses array");
            foreach (var license in deserializedLicenses)
            {
#endif
                if (!TryGetLicenseKey(license, out var key)) continue;
                if (key is null) continue;
                // Add without text first; text fetched in parallel later
                _licenses.AddOrUpdate(key, license, (_, _) => license);
            }

            LoggerMethods.LogStreamingDeserializationCompletedLoadedCountLicenses(_logger, _licenses.Count);
        }
        catch (Exception)
        {
            LoggerMethods.LogStreamingDeserializationFailedFallingBackToArrayDeserialization(_logger);
            // Ignore streaming errors; attempt JSON array fallback below
        }

        // Fallback: if streaming returned nothing, try parsing as a JSON array
        if (_licenses.IsEmpty)
        {
            LoggerMethods.LogAttemptingFallbackArrayDeserialization(_logger);
            try
            {
#if NET10_0_OR_GREATER
                await using var stream = await _httpClient.GetStreamAsync(LicensesPath, cancellationToken)
                    .ConfigureAwait(false);
#else
                using var stream = await _httpClient.GetStreamAsync(LicensesPath)
                    .ConfigureAwait(false);
#endif

                var arr = await JsonSerializer
                    .DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (arr is not null)
                {
                    LoggerMethods.LogFallbackDeserializationSuccessfulProcessingCountLicenses(_logger, arr.Length);
                    foreach (var lic in arr)
                    {
                        if (!TryGetLicenseKey(lic, out var k)) continue;
                        if (k is null) continue;
                        _licenses.AddOrUpdate(k, lic, (_, _) => lic);
                    }
                }
            }
            catch (Exception)
            {
                LoggerMethods.LogFallbackDeserializationFailed(_logger);
                // still fail-safe; leave dictionary empty
            }
        }

        // Bounded parallelism for fetching license texts
        var maxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount);
        LoggerMethods.LogFetchingLicenseTextsWithMaxParallelismOfMaxparallelism(_logger, maxDegreeOfParallelism);
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
                catch (Exception)
                {
                    LoggerMethods.LogFailedToFetchLicenseTextForLicensenameSpdxSpdxid(_logger, license.Name,
                        license.SpdxId);
                    // keep going; leave LicenseText as null on failure
                }
                finally
                {
                    throttler.Release();
                }
            }, cancellationToken));
        }

        LoggerMethods.LogInitiatedCountLicenseTextFetchOperations(_logger, textFetchCount);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
            LoggerMethods.LogAllLicenseTextFetchOperationsCompleted(_logger);
        }
        catch (Exception)
        {
            LoggerMethods.LogSomeLicenseTextFetchOperationsFailed(_logger);
            // Ignore aggregate exceptions from cancelled tasks; fail-safe behavior
        }

        list.AddRange(_licenses.Values);
        // Sort for deterministic order (by SPDX id if available, else by name)
        list.Sort(static (a, b) =>
            string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));
        Licenses = list;

        sw.Stop();
        LoggerMethods.LogSuccessfullyLoadedCountLicensesInElapsedmsMs(_logger, Licenses.Count, sw.ElapsedMilliseconds);
        return Licenses;
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetAllLicenses()
    {
        return GetAllLicensesAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> SearchAsync(string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            LoggerMethods.LogSearchCalledWithEmptyQueryReturningEmptyResult(_logger);
            return Array.Empty<OsiLicense>();
        }

        LoggerMethods.LogSearchingForLicensesMatchingQueryQuery(_logger, query);
        await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
        var q = query.Trim();
        var results = Licenses.Where(l =>
                (!string.IsNullOrEmpty(l.Name) && l.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(l.Id) && l.Id.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        LoggerMethods.LogSearchForQueryReturnedCountResultS(_logger, query, results.Length);
        return results;
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> Search(string query)
    {
        return SearchAsync(query).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<OsiLicense?> GetBySpdxAsync(string spdxId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spdxId))
        {
            LoggerMethods.LogGetbyspdxCalledWithEmptySpdxId(_logger);
            return null;
        }

        LoggerMethods.LogLookingUpLicenseBySpdxIdSpdxid(_logger, spdxId);
        await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
        // Try fast path via dictionary (keys prefer SPDX when available)
        if (_licenses.TryGetValue(spdxId, out var lic))
        {
            LoggerMethods.LogFoundLicenseLicensenameViaDictionaryLookup(_logger, lic.Name);
            return lic;
        }

        // Fallback scan
        var result = Licenses.FirstOrDefault(l => string.Equals(l.SpdxId, spdxId, StringComparison.OrdinalIgnoreCase));
        if (result != null)
            LoggerMethods.LogFoundLicenseLicensenameViaFallbackScan(_logger, result.Name);
        else
            LoggerMethods.LogLicenseWithSpdxIdSpdxidNotFound(_logger, spdxId);

        return result;
    }

    /// <inheritdoc />
    public OsiLicense? GetBySpdx(string spdxId)
    {
        return GetBySpdxAsync(spdxId).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByNameAsync(string name,
        CancellationToken cancellationToken = default)
    {
        return FetchFilteredAsync("name", name, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(string keyword,
        CancellationToken cancellationToken = default)
    {
        return FetchFilteredAsync("keyword", keyword, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(OsiLicenseKeyword keyword,
        CancellationToken cancellationToken = default)
    {
        return FetchFilteredAsync("keyword", OsiLicenseKeywordMapping.ToApiValue(keyword), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByStewardAsync(string steward,
        CancellationToken cancellationToken = default)
    {
        return FetchFilteredAsync("steward", steward, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesBySpdxPatternAsync(string spdxPattern,
        CancellationToken cancellationToken = default)
    {
        return FetchFilteredAsync("spdx", spdxPattern, cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesByName(string name)
    {
        return GetLicensesByNameAsync(name).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesByKeyword(string keyword)
    {
        return GetLicensesByKeywordAsync(keyword).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesByKeyword(OsiLicenseKeyword keyword)
    {
        return GetLicensesByKeywordAsync(keyword).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesBySteward(string steward)
    {
        return GetLicensesByStewardAsync(steward).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetLicensesBySpdxPattern(string spdxPattern)
    {
        return GetLicensesBySpdxPatternAsync(spdxPattern).GetAwaiter().GetResult();
    }

    private async Task<IReadOnlyList<OsiLicense>> FetchFilteredAsync(string paramName, string paramValue,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paramValue))
        {
            LoggerMethods.LogFetchfilteredCalledWithEmptyValueForParameterParamname(_logger, paramName);
            return Array.Empty<OsiLicense>();
        }

        LoggerMethods.LogFetchingLicensesFilteredByParamnameParamvalue(_logger, paramName, paramValue);
        var sw = Stopwatch.StartNew();

        var encoded = Uri.EscapeDataString(paramValue);
        if (string.Equals(paramName, "spdx", StringComparison.OrdinalIgnoreCase))
            // Preserve '*' wildcard per API spec
            encoded = encoded.Replace("%2A", "*");

        var request = $"{LicensesPath}?{paramName}={encoded}";
        LoggerMethods.LogRequestUrlRequesturl(_logger, request);

        OsiLicense[]? items;
        try
        {
#if NET10_0_OR_GREATER
            await
#endif
                using var stream = await _httpClient.GetStreamAsync(request
#if NET10_0_OR_GREATER
                    , cancellationToken
#endif
                ).ConfigureAwait(false);
#if NET10_0_OR_GREATER
            items = await JsonSerializer
                .DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
#else
            items =
                await JsonSerializer.DeserializeAsync<OsiLicense[]>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
#endif
            LoggerMethods.LogDeserializedCountFilteredLicenses(_logger, items?.Length ?? 0);
        }
        catch (Exception)
        {
            LoggerMethods.LogFailedToFetchFilteredLicensesByParamnameParamvalue(_logger, paramName, paramValue);
            return Array.Empty<OsiLicense>();
        }

        if (items is null || items.Length == 0)
        {
            LoggerMethods.LogNoLicensesFoundForParamnameParamvalue(_logger, paramName, paramValue);
            return Array.Empty<OsiLicense>();
        }

        // Map into a temp list and enrich with license text
        var list = new List<OsiLicense>(items.Length);
        foreach (var license in items)
        {
            if (TryGetLicenseKey(license, out var key)) _licenses.AddOrUpdate(key!, license, (_, _) => license);

            list.Add(license);
        }

        // Enrich with license text in parallel (bounded)
        var maxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount);
        LoggerMethods.LogEnrichingCountFilteredLicensesWithTextMaxParallelismMaxparallelism(_logger, list.Count,
            maxDegreeOfParallelism);
        var throttler = new SemaphoreSlim(maxDegreeOfParallelism);
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
                catch (Exception)
                {
                    LoggerMethods.LogFailedToFetchLicenseTextForLicensenameSpdxSpdxid(_logger, lic.Name, lic.SpdxId);
                    /* fail-safe per item */
                }
                finally
                {
                    throttler.Release();
                }
            }, cancellationToken));
        }

        LoggerMethods.LogInitiatedCountLicenseTextFetchOperationsForFilteredResults(_logger, textFetchCount);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
            LoggerMethods.LogAllLicenseTextFetchOperationsCompletedForFilteredResults(_logger);
        }
        catch (Exception)
        {
            LoggerMethods.LogSomeLicenseTextFetchOperationsFailedForFilteredResults(_logger);
            /* ignore */
        }

        // Deterministic order
        list.Sort(static (a, b) =>
            string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));

        sw.Stop();
        LoggerMethods.LogFetchedAndEnrichedCountLicensesFilteredByParamnameParamvalueInElapsedmsMs(_logger, list.Count,
            paramName, paramValue, sw.ElapsedMilliseconds);

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
        LoggerMethods.LogDisposingOsilicensesclient(_logger);
        GC.SuppressFinalize(this);
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
            LoggerMethods.LogDisposedOwnedHttpclient(_logger);
        }

        _initGate.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        LoggerMethods.LogDisposingOsilicensesclientAsynchronously(_logger);
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
                LoggerMethods.LogDisposedOwnedHttpclient(_logger);
            }

            await Task.CompletedTask;
        }
    }

    private static void EnsureDefaultHeaders(HttpClient client)
    {
        // Accept JSON by default
        if (client.DefaultRequestHeaders.Accept.Count == 0)
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (client.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("OpenSourceInitiative-LicenseApi-Client", version));
        }
    }
}