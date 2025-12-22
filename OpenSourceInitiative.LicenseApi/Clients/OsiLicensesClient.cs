using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSourceInitiative.LicenseApi.Converter;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Log;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Exceptions;

namespace OpenSourceInitiative.LicenseApi.Clients;

/// <summary>
///     High-level implementation of <see cref="IOsiLicensesClient" /> that wraps an <see cref="IOsiClient"/>.
/// </summary>
public class OsiLicensesClient : IOsiLicensesClient
{
    private readonly ILogger<OsiLicensesClient> _logger;
    private readonly IOsiClient _osiClient;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private volatile bool _initialized;

    /// <summary>
    ///     Read-only, fail-safe view of the last loaded licenses snapshot.
    /// </summary>
    public IReadOnlyList<OsiLicense> Licenses { get; private set; } = Array.Empty<OsiLicense>();

    /// <summary>
    ///     Creates a client that wraps the provided <paramref name="osiClient" />.
    /// </summary>
    public OsiLicensesClient(IOsiClient osiClient, ILogger<OsiLicensesClient>? logger = null)
    {
        _osiClient = osiClient ?? throw new ArgumentNullException(nameof(osiClient));
        _logger = logger ?? NullLogger<OsiLicensesClient>.Instance;
    }

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
        catch (OperationCanceledException) { throw; }
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
    public void Initialize() => InitializeAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<OsiLicense>();
        await foreach (var license in _osiClient.GetAllLicensesAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (license != null) list.Add(license);
        }

        // Maintain deterministic order
        list.Sort(static (a, b) => string.Compare(a.SpdxId ?? a.Name, b.SpdxId ?? b.Name, StringComparison.OrdinalIgnoreCase));
        Licenses = list.AsReadOnly();
        return Licenses;
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> GetAllLicenses() => GetAllLicensesAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<OsiLicense>();
        
        await InitializeAsync(cancellationToken);
        var q = query.Trim();
        return Licenses.Where(l => 
            (l.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) || 
            (l.Id?.Contains(q, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<OsiLicense> Search(string query) => SearchAsync(query).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<OsiLicense?> GetBySpdxAsync(string spdxId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spdxId)) return null;
        var results = await _osiClient.GetBySpdxIdAsync(spdxId);
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public OsiLicense? GetBySpdx(string spdxId) => GetBySpdxAsync(spdxId).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetLicensesByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Array.Empty<OsiLicense>();
        return (await _osiClient.GetByNameAsync(name)).Where(x => x != null).Cast<OsiLicense>().ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return Array.Empty<OsiLicense>();
        if (!OsiLicenseKeywordMapping.TryParse(keyword, out var value)) return Array.Empty<OsiLicense>();
        return (await _osiClient.GetByKeywordAsync(value)).Where(x => x != null).Cast<OsiLicense>().ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(OsiLicenseKeyword keyword, CancellationToken cancellationToken = default)
        => GetLicensesByKeywordAsync(OsiLicenseKeywordMapping.ToApiValue(keyword), cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetLicensesByStewardAsync(string steward, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steward)) return Array.Empty<OsiLicense>();
        return (await _osiClient.GetByStewardAsync(steward)).Where(x => x != null).Cast<OsiLicense>().ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OsiLicense>> GetLicensesBySpdxPatternAsync(string spdxPattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spdxPattern)) return Array.Empty<OsiLicense>();
        return (await _osiClient.GetBySpdxIdAsync(spdxPattern)).Where(x => x != null).Cast<OsiLicense>().ToList();
    }

    // Synchronous wrappers
    public IReadOnlyList<OsiLicense> GetLicensesByName(string name) => GetLicensesByNameAsync(name).GetAwaiter().GetResult();
    public IReadOnlyList<OsiLicense> GetLicensesByKeyword(string keyword) => GetLicensesByKeywordAsync(keyword).GetAwaiter().GetResult();
    public IReadOnlyList<OsiLicense> GetLicensesByKeyword(OsiLicenseKeyword keyword) => GetLicensesByKeywordAsync(keyword).GetAwaiter().GetResult();
    public IReadOnlyList<OsiLicense> GetLicensesBySteward(string steward) => GetLicensesByStewardAsync(steward).GetAwaiter().GetResult();
    public IReadOnlyList<OsiLicense> GetLicensesBySpdxPattern(string spdxPattern) => GetLicensesBySpdxPatternAsync(spdxPattern).GetAwaiter().GetResult();

    public void Dispose() => _initGate.Dispose();
    public async ValueTask DisposeAsync() => _initGate.Dispose();
}