#if !NETSTANDARD2_0
using System.Net.Http.Json;
#else
using System.Text.Json;
#endif
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSourceInitiative.LicenseApi.Converter;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Options;

namespace OpenSourceInitiative.LicenseApi.Clients;

/// <summary>
/// Provides functionality to interact with the Open Source Initiative (OSI) License API.
/// The OsiClient allows searching, retrieving, and enumerating OSI-approved licenses
/// using various parameters such as SPDX ID, name, keyword, and steward.
/// </summary>
public sealed class OsiClient : IOsiClient
{
    private const string AllLicensesEndpoint = "license";
    private const string SingleLicenseEndpoint = "license/{0}";
    private const string NameFilter = "name={0}";
    private const string KeywordFilter = "keyword={0}";
    private const string StewardFilter = "steward={0}";
    private const string SpdxFilter = "spdx={0}";

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly ILogger<OsiClient> _logger;

    public OsiClient(
        ILogger<OsiClient>? logger = null,
        OsiClientOptions? options = null,
        HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.ConfigureForLicenseApi(options ?? new OsiClientOptions());
        _disposeHttpClient = httpClient == null;
        _logger = logger ?? NullLogger<OsiClient>.Instance;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable([EnumeratorCancellation] CancellationToken token = default)
    {
#if !NETSTANDARD2_0
        await using var httpContentStream = await _httpClient.GetStreamAsync(AllLicensesEndpoint, token);
#else
        using var httpContentStream = await _httpClient.GetStreamAsync(AllLicensesEndpoint);
#endif
        
        await foreach(var license in JsonSerializer.DeserializeAsyncEnumerable<OsiLicense?>(httpContentStream, cancellationToken: token))
        {
            _logger.LogTrace("Fetched license {License}", license);
            if (license is null)
            {
                yield return license;
                continue;
            }

            try
            {
                license.LicenseText = await _httpClient.GetLicenseTextAsync(license, cancellationToken: token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to fetch license text for {License}", license);
            }
            yield return license;
        }
    }

    private enum LicenseEndpointType
    {
        SpdxId,
        Name,
        Keyword,
        Steward
    }

    /// <inheritdoc />
    public async Task<OsiLicense?> GetByOsiIdAsync(string id, CancellationToken token = default)
    {
        var license =
#if !NETSTANDARD2_0
            await _httpClient.GetFromJsonAsync<OsiLicense?>(string.Format(SingleLicenseEndpoint, id), cancellationToken: token);
#else
            await JsonSerializer.DeserializeAsync<OsiLicense?>((await _httpClient.GetStreamAsync(string.Format(SingleLicenseEndpoint, id))), cancellationToken: token);
#endif
        if (license is null) return null;
        license.LicenseText = await _httpClient.GetLicenseTextAsync(license, cancellationToken: token);
        return license;
    }

    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id, CancellationToken token = default) => GetLicenseBy(LicenseEndpointType.SpdxId, id, token);

    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken token = default) => GetLicenseBy(LicenseEndpointType.Name, name, token);

    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword, CancellationToken token = default) =>
        GetLicenseBy(LicenseEndpointType.Keyword, OsiLicenseKeywordMapping.ToApiValue(keyword), token);

    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward, CancellationToken token = default) =>
        GetLicenseBy(LicenseEndpointType.Steward, steward, token);

    /// <summary>
    /// Retrieves a collection of licenses matching the specified criteria.
    /// </summary>
    /// <param name="type">
    /// The type of filter to apply when searching for licenses. This determines the query parameter used in the API request.
    /// </param>
    /// <param name="value">
    /// The value to filter by, corresponding to the selected filter type (e.g., SPDX ID, name, keyword, or steward).
    /// </param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable collection of <see cref="OsiLicense"/> objects matching the specified criteria.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an invalid <paramref name="type"/> is provided.
    /// </exception>
    private async Task<IEnumerable<OsiLicense?>> GetLicenseBy(LicenseEndpointType type, string value, CancellationToken token = default)
    {
        var query = string.Join("?", AllLicensesEndpoint, string.Format(type switch
        {
            LicenseEndpointType.SpdxId => SpdxFilter,
            LicenseEndpointType.Name => NameFilter,
            LicenseEndpointType.Keyword => KeywordFilter,
            LicenseEndpointType.Steward => StewardFilter,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        }, value));
        _logger.LogTrace("Querying with {Query}", string.Join("", _httpClient.BaseAddress, query));

        if (
#if !NETSTANDARD2_0
            (await _httpClient.GetFromJsonAsync<OsiLicense?[]?>(query, cancellationToken: token)) is { } remoteLicenses
#else
            (await JsonSerializer.DeserializeAsync<OsiLicense?[]?>(await _httpClient.GetStreamAsync(query), cancellationToken: token) is { } remoteLicenses)
#endif
        )
        {
           var listOfLicensesWithLicenseText = new List<OsiLicense>();
           foreach (var license in remoteLicenses)
           {
               if(license is null) continue;
               license.LicenseText = await _httpClient.GetLicenseTextAsync(license, cancellationToken: token);
               listOfLicensesWithLicenseText.Add(license);
           }
           return listOfLicensesWithLicenseText;
        }
        return [];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeHttpClient)
            _httpClient.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposeHttpClient)
            _httpClient.Dispose();
        return new ValueTask(Task.CompletedTask);
    }
}