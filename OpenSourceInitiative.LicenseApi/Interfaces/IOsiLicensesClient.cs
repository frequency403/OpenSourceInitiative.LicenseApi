using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;
// ReSharper disable UnusedMemberInSuper.Global

namespace OpenSourceInitiative.LicenseApi.Interfaces;

/// <summary>
///     Contract for a lightweight client that interacts with the Open Source Initiative (OSI) License API.
/// </summary>
/// <remarks>
///     The client is designed to be resilient:
///     - Fetches the catalog of OSI licenses and extracts human‑readable license text from the HTML pages.
///     - Maintains an in‑memory, thread‑safe cache of licenses once loaded (if caching is enabled); subsequent queries operate on this snapshot.
///     - Network calls throw library-specific exceptions on failure.
///     - Provides both asynchronous methods and synchronous counterparts for convenience.
/// </remarks>
public interface IOsiLicensesClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Gets the current immutable snapshot of licenses. This list is populated after the first successful fetch.
    /// </summary>
    IReadOnlyList<OsiLicense> Licenses { get; }

    // Async API
    /// <summary>
    ///     Ensures the internal cache is initialized by fetching the license catalog.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the complete catalog of licenses. Populates the internal cache on the first call.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of licenses (possibly empty on failures).</returns>
    Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves licenses whose name contains the specified text (server-side filter).
    /// </summary>
    /// <param name="name">Substring to match in license names (case-insensitive on server side).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OsiLicense>> GetLicensesByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves licenses filtered by a classification keyword.
    /// </summary>
    /// <param name="keyword">Keyword value as defined by OSI (e.g., "popular-strong-community").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(string keyword,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves licenses filtered by a classification keyword using the strongly-typed enum.
    /// </summary>
    /// <param name="keyword">Keyword enum value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OsiLicense>> GetLicensesByKeywordAsync(OsiLicenseKeyword keyword,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves licenses filtered by steward organization.
    /// </summary>
    /// <param name="steward">Steward slug (e.g., "eclipse-foundation").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OsiLicense>> GetLicensesByStewardAsync(string steward,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves licenses filtered by SPDX identifier using optional wildcard patterns.
    /// </summary>
    /// <param name="spdxPattern">SPDX filter supporting '*' wildcards (e.g., "gpl*", "*bsd").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OsiLicense>> GetLicensesBySpdxPatternAsync(string spdxPattern,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Searches the cached list of licenses by name or identifier (case-insensitive, partial match).
    /// </summary>
    /// <param name="query">Text to search for in <see cref="OsiLicense.Name" /> or <see cref="OsiLicense.Id" />.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching licenses. Returns an empty list if <paramref name="query" /> is null/whitespace or on failure.</returns>
    Task<IReadOnlyList<OsiLicense>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves a single license by SPDX identifier (e.g., "MIT").
    /// </summary>
    /// <param name="spdxId">SPDX identifier to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching license or null if not found.</returns>
    Task<OsiLicense?> GetBySpdxAsync(string spdxId, CancellationToken cancellationToken = default);

    // Synchronous counterparts
    /// <summary>
    ///     Synchronous wrapper for <see cref="InitializeAsync(CancellationToken)" />.
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Synchronous wrapper for <see cref="GetAllLicensesAsync(CancellationToken)" />.
    /// </summary>
    IReadOnlyList<OsiLicense> GetAllLicenses();

    /// <summary>
    ///     Synchronous wrapper for <see cref="GetLicensesByNameAsync" />.
    /// </summary>
    IReadOnlyList<OsiLicense> GetLicensesByName(string name);

    /// <summary>
    ///     Synchronous wrapper for <see cref="GetLicensesByKeywordAsync(System.String, System.Threading.CancellationToken)" />
    ///     .
    /// </summary>
    IReadOnlyList<OsiLicense> GetLicensesByKeyword(string keyword);

    /// <summary>
    ///     Synchronous wrapper for
    ///     <see cref="GetLicensesByKeywordAsync(OsiLicenseKeyword,System.Threading.CancellationToken)" />.
    /// </summary>
    IReadOnlyList<OsiLicense> GetLicensesByKeyword(OsiLicenseKeyword keyword);

    /// <summary>
    ///     Synchronous wrapper for <see cref="GetLicensesByStewardAsync" />.
    /// </summary>
    IReadOnlyList<OsiLicense> GetLicensesBySteward(string steward);

    /// <summary>
    ///     Synchronous wrapper for <see cref="GetLicensesBySpdxPatternAsync" />.
    /// </summary>
    IReadOnlyList<OsiLicense> GetLicensesBySpdxPattern(string spdxPattern);

    /// <summary>
    ///     Synchronous wrapper for <see cref="SearchAsync(string, CancellationToken)" />.
    /// </summary>
    IReadOnlyList<OsiLicense> Search(string query);

    /// <summary>
    ///     Synchronous wrapper for <see cref="GetBySpdxAsync(string, CancellationToken)" />.
    /// </summary>
    OsiLicense? GetBySpdx(string spdxId);
}