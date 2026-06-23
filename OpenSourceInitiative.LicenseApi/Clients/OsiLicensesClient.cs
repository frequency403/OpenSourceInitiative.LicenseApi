using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSourceInitiative.LicenseApi.Converter;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Exceptions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Log;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Clients;

/// <summary>
///     High-level implementation of <see cref="IOsiLicensesClient" /> that wraps an <see cref="IOsiLicensesClient" />.
/// </summary>
[Obsolete("Use IOsiClient instead.")]
[ExcludeFromCodeCoverage]
public sealed class OsiLicensesClient : IOsiLicensesClient
{
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private readonly ILogger<OsiLicensesClient> _logger;
    private readonly IOsiClient _osiClient;
    private volatile bool _initialized;

    /// <summary>
    ///     Creates a client that wraps the provided <paramref name="osiClient" />.
    /// </summary>
    public OsiLicensesClient(IOsiClient osiClient, ILogger<OsiLicensesClient>? logger = null)
    {
        _osiClient = osiClient ?? throw new ArgumentNullException(nameof(osiClient));
        _logger = logger ?? NullLogger<OsiLicensesClient>.Instance;
    }

    /// <summary>
    ///     Read-only, fail-safe view of the last loaded licenses snapshot.
    /// </summary>
    public IReadOnlyList<OsiLicense> Licenses { get; private set; } = [];

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        LoggerMethods.LogAcquiringInitializationLock(_logger);
        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            LoggerMethods.LogInitializingOsilicensesclient(_logger);
            await GetAllLicensesAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
            LoggerMethods.LogOsilicensesclientInitializationCompletedSuccessfully(_logger);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LoggerMethods.LogFailedToInitializeOsilicensesclient(_logger);
            throw new OsiInitializationException("Failed to initialize OsiLicensesClient", ex);
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<OsiLicense>();
        await foreach (var license in _osiClient.GetAllLicensesAsyncEnumerable().WithCancellation(cancellationToken))
            if (license != null)
                list.Add(license);

        // Maintain deterministic order
        list.Sort(static (a, b) =>
            string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));
        Licenses = list.AsReadOnly();
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
        if (string.IsNullOrWhiteSpace(query)) return [];

        await InitializeAsync(cancellationToken);
        var q = query.Trim();
        return Licenses.Where(l =>
                l.Name.Contains(q, StringComparison.OrdinalIgnoreCase)||
                l.Id.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> Search(string query)
    {
        return SearchAsync(query).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<OsiLicense?> GetBySpdxAsync(string spdxId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spdxId)) return null;
        var results = await _osiClient.GetBySpdxIdAsync(spdxId, cancellationToken);
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public OsiLicense? GetBySpdx(string spdxId)
    {
        return GetBySpdxAsync(spdxId).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetLicensesByNameAsync(string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return [];
        return (await _osiClient.GetByNameAsync(name, cancellationToken)).Where(x => x != null).Cast<OsiLicense>().ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(string keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return [];
        if (!OsiLicenseKeywordMapping.TryParse(keyword, out var value)) return [];
        return (await _osiClient.GetByKeywordAsync(value, cancellationToken)).Where(x => x != null).Cast<OsiLicense>().ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(OsiLicenseKeyword keyword,
        CancellationToken cancellationToken = default)
    {
        return GetLicensesByKeywordAsync(OsiLicenseKeywordMapping.ToApiValue(keyword), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetLicensesByStewardAsync(string steward,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steward)) return [];
        return (await _osiClient.GetByStewardAsync(steward, cancellationToken)).Where(x => x != null).Cast<OsiLicense>().ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetLicensesBySpdxPatternAsync(string spdxPattern,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spdxPattern)) return [];
        return (await _osiClient.GetBySpdxIdAsync(spdxPattern, cancellationToken)).Where(x => x != null).Cast<OsiLicense>().ToList();
    }

    // Synchronous wrappers
    public IReadOnlyList<OsiLicense> GetLicensesByName(string name)
    {
        return GetLicensesByNameAsync(name).GetAwaiter().GetResult();
    }

    public IReadOnlyList<OsiLicense> GetLicensesByKeyword(string keyword)
    {
        return GetLicensesByKeywordAsync(keyword).GetAwaiter().GetResult();
    }

    public IReadOnlyList<OsiLicense> GetLicensesByKeyword(OsiLicenseKeyword keyword)
    {
        return GetLicensesByKeywordAsync(keyword).GetAwaiter().GetResult();
    }

    public IReadOnlyList<OsiLicense> GetLicensesBySteward(string steward)
    {
        return GetLicensesByStewardAsync(steward).GetAwaiter().GetResult();
    }

    public IReadOnlyList<OsiLicense> GetLicensesBySpdxPattern(string spdxPattern)
    {
        return GetLicensesBySpdxPatternAsync(spdxPattern).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _initGate.Dispose();
    }
    
    public ValueTask DisposeAsync()
    {
        _initGate.Dispose();
#if !NETSTANDARD2_0
        return ValueTask.CompletedTask;
#else
        return new ValueTask();
#endif
    }
}