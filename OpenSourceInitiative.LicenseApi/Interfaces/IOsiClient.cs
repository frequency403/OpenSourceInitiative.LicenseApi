using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Interfaces;

/// <summary>
///     Client for interacting with the Open Source Initiative (OSI) License API.
///     Provides methods to retrieve, filter, and stream license information.
/// </summary>
public interface IOsiClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Retrieves all available licenses from the OSI API as an asynchronous stream.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>An <see cref="IAsyncEnumerable{OsiLicense}"/> of all registered licenses.</returns>
    IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable(CancellationToken token = default);

    /// <summary>
    ///     Retrieves a single license by its unique OSI identifier.
    /// </summary>
    /// <param name="id">The unique OSI ID (e.g., "mit", "apache2").</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the 
    ///     <see cref="OsiLicense"/> if found; otherwise, <c>null</c>.
    /// </returns>
    Task<OsiLicense?> GetByOsiIdAsync(string id, CancellationToken token = default);

    /// <summary>
    ///     Filters licenses by their SPDX identifier. <br />
    ///     Supports wildcard matching using the <c>*</c> character for flexible pattern matching.
    ///     <list type="table">
    ///         <listheader>
    ///             <description><b>Wildcard Pattern</b></description>
    ///         </listheader>
    ///         <item>
    ///             <term><c>prefix*</c></term>
    ///             <description>Find licenses that start with the prefix (e.g., <c>GPL*</c>).</description>
    ///         </item>
    ///         <item>
    ///             <term><c>*suffix</c></term>
    ///             <description>Find licenses that end with the suffix (e.g., <c>*-2.0</c>).</description>
    ///         </item>
    ///         <item>
    ///             <term><c>*contains*</c></term>
    ///             <description>Find licenses that contain the text anywhere (e.g., <c>*Apache*</c>).</description>
    ///         </item>
    ///         <item>
    ///             <term><c>exact</c></term>
    ///             <description>Find licenses with an exact match (no wildcards).</description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <param name="id">The SPDX identifier or pattern to search for.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A collection of matching <see cref="OsiLicense"/> objects.</returns>
    Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id, CancellationToken token = default);

    /// <summary>
    ///     Filters licenses by their human-readable name.
    /// </summary>
    /// <param name="name">The name or partial name to search for.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A collection of matching <see cref="OsiLicense"/> objects.</returns>
    Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken token = default);

    /// <summary>
    ///     Filters licenses based on specific classification keywords assigned by the OSI.
    /// </summary>
    /// <param name="keyword">The keyword token (<see cref="OsiLicenseKeyword"/>).</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A collection of matching <see cref="OsiLicense"/> objects.</returns>
    Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword, CancellationToken token = default);

    /// <summary>
    ///     Filters licenses based on the steward (organization or entity) responsible for the license.
    /// </summary>
    /// <param name="steward">The name of the steward to search for.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A collection of matching <see cref="OsiLicense"/> objects.</returns>
    Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward, CancellationToken token = default);
}