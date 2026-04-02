using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;
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
///     Provides functionality to interact with the Open Source Initiative (OSI) License API.
///     The OsiClient allows searching, retrieving, and enumerating OSI-approved licenses
///     using various parameters such as SPDX ID, name, keyword, and steward.
/// </summary>
public sealed class OsiClient : IOsiClient
{
    private const string LicenseEndpoint = "license";
    private readonly Uri _baseAddress;
    private readonly bool _disposeHttpClient;

    private readonly HttpClient _httpClient;
    private readonly ILogger<OsiClient> _logger;

    public OsiClient(
        ILogger<OsiClient>? logger = null,
        OsiClientOptions? options = null,
        HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        var option = options ?? new OsiClientOptions();
        _baseAddress = new Uri(option.BaseAddress, LicenseEndpoint);
        _httpClient.ConfigureForLicenseApi(option);
        _disposeHttpClient = httpClient == null;
        _logger = logger ?? NullLogger<OsiClient>.Instance;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var httpResponse = await _httpClient.GetAsync(_baseAddress, token);
        try
        {
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch licenses from {Endpoint}", _baseAddress);
            throw;
        }

#if !NETSTANDARD2_0
        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync(token);
#else
        using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
#endif

        await foreach (var license in JsonSerializer.DeserializeAsyncEnumerable<OsiLicense?>(responseStream,
                           cancellationToken: token))
        {
            _logger.LogTrace("Fetched license {License}", license);
            if (license is null)
            {
                yield return license;
                continue;
            }

            try
            {
                license.LicenseText = await _httpClient.GetLicenseTextAsync(license, token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to fetch license text for {License}", license);
            }

            yield return license;
        }
    }

    /// <inheritdoc />
    public async Task<OsiLicense?> GetByOsiIdAsync(string id, CancellationToken token = default)
    {
        if (_httpClient.BaseAddress is null)
            throw new InvalidOperationException("Base address is not set");
        var uri = new Uri(_baseAddress, id);
        var httpResponse = await _httpClient.GetAsync(uri, token);
        try
        {
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve license with ID {Id}", id);
            throw;
        }

#if !NETSTANDARD2_0
        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync(token);
#else
        using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
#endif

        if (await JsonSerializer.DeserializeAsync<OsiLicense?>(responseStream, cancellationToken: token) is not
            { } license) return null;
        license.LicenseText = await _httpClient.GetLicenseTextAsync(license, token);
        return license;
    }

    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id, CancellationToken token = default)
    {
        return GetLicenseBy(LicenseEndpointType.Spdx, id, token);
    }

    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken token = default)
    {
        return GetLicenseBy(LicenseEndpointType.Name, name, token);
    }

    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword,
        CancellationToken token = default)
    {
        return GetLicenseBy(LicenseEndpointType.Keyword, OsiLicenseKeywordMapping.ToApiValue(keyword), token);
    }

    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward, CancellationToken token = default)
    {
        return GetLicenseBy(LicenseEndpointType.Steward, steward, token);
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

    /// <summary>
    ///     Retrieves a collection of licenses matching the specified criteria.
    /// </summary>
    /// <param name="type">
    ///     The type of filter to apply when searching for licenses. This determines the query parameter used in the API
    ///     request.
    /// </param>
    /// <param name="value">
    ///     The value to filter by, corresponding to the selected filter type (e.g., SPDX ID, name, keyword, or steward).
    /// </param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains an enumerable collection of
    ///     <see cref="OsiLicense" /> objects matching the specified criteria.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when an invalid <paramref name="type" /> is provided.
    /// </exception>
    private async Task<IEnumerable<OsiLicense?>> GetLicenseBy(LicenseEndpointType type, string value,
        CancellationToken token = default)
    {
        if (_httpClient.BaseAddress is null)
            throw new InvalidOperationException("Base address is not set");

        var uriBuilder = new UriBuilder(_baseAddress);
        var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);
        queryString.Add(type.ToString().ToLower(), value);
        uriBuilder.Query = queryString.ToString();

        _logger.LogTrace("Querying with {Query}", uriBuilder.Uri);

        var httpResponse = await _httpClient.GetAsync(uriBuilder.Uri, token);
        try
        {
            httpResponse.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch licenses from {Endpoint}", LicenseEndpoint);
            throw;
        }

#if !NETSTANDARD2_0
        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync(token);
#else
        using var responseStream = await httpResponse.Content.ReadAsStreamAsync();
#endif

        if (await JsonSerializer.DeserializeAsync<OsiLicense?[]?>(responseStream, cancellationToken: token) is not
            { } remoteLicenses)
            return [];
        var listOfLicensesWithLicenseText = new List<OsiLicense>();
        foreach (var license in remoteLicenses)
        {
            if (license is null) continue;
            license.LicenseText = await _httpClient.GetLicenseTextAsync(license, token);
            listOfLicensesWithLicenseText.Add(license);
        }

        return listOfLicensesWithLicenseText;
    }

    private enum LicenseEndpointType
    {
        Spdx,
        Name,
        Keyword,
        Steward
    }
}