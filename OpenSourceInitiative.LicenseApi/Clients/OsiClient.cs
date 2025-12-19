#if !NETSTANDARD2_0
using System.Net.Http.Json;
#endif
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Options;

namespace OpenSourceInitiative.LicenseApi.Clients;

internal class OsiClient(
    ILogger<OsiClient>? logger = null,
    OsiClientOptions? options = null,
    HttpClient? httpClient = null)
    : IOsiClient
{
    private const string AllLicensesEndpoint = "license";
    private const string SingleLicenseEndpoint = "/{0}";
    private const string NameFilter = "name={0}";
    private const string KeywordFilter = "keyword={0}";
    private const string StewardFilter = "steward={0}";
    private const string SpdxFilter = "spdx={0}";
    
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient
    {
        BaseAddress = options?.BaseAddress ?? new OsiClientOptions().BaseAddress
    };
    private readonly ILogger<OsiClient> _logger = logger ?? NullLogger<OsiClient>.Instance;

    /// <inheritdoc />
#if !NETSTANDARD2_0
     public IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable() =>
        _httpClient.GetFromJsonAsAsyncEnumerable<OsiLicense>(AllLicensesEndpoint);
#else
    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable()
    {
        await foreach (var license in JsonSerializer.DeserializeAsyncEnumerable<OsiLicense?>(
                           await (await _httpClient.GetAsync(AllLicensesEndpoint)).Content.ReadAsStreamAsync()))
        {
            _logger.LogTrace("Fetched license {License}", license);
            yield return license;
        }
    }
#endif
    private enum LicenseEndpointType
    {
        SpdxId,
        Name,
        Keyword,
        Steward
    }

    /// <inheritdoc />
#if !NETSTANDARD2_0
public Task<OsiLicense?> GetByOsiIdAsync(string id)
    {
        return _httpClient.GetFromJsonAsync<OsiLicense?>(string.Format(SingleLicenseEndpoint, id));
        #else
    public async Task<OsiLicense?> GetByOsiIdAsync(string id)
    {
        return await JsonSerializer.DeserializeAsync<OsiLicense?>((await _httpClient.GetStreamAsync(string.Format(SingleLicenseEndpoint, id))));
#endif
    }
    
    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id) => GetLicenseBy(LicenseEndpointType.SpdxId, id);
    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name) => GetLicenseBy(LicenseEndpointType.Name, name);
    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(string keyword) => GetLicenseBy(LicenseEndpointType.Keyword, keyword);
    /// <inheritdoc />
    public Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward) => GetLicenseBy(LicenseEndpointType.Steward, steward);

    /// <summary>
    /// Retrieves a collection of licenses matching the specified criteria.
    /// </summary>
    /// <param name="type">
    /// The type of filter to apply when searching for licenses. This determines the query parameter used in the API request.
    /// </param>
    /// <param name="value">
    /// The value to filter by, corresponding to the selected filter type (e.g., SPDX ID, name, keyword, or steward).
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable collection of <see cref="OsiLicense"/> objects matching the specified criteria.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an invalid <paramref name="type"/> is provided.
    /// </exception>
    private async Task<IEnumerable<OsiLicense?>> GetLicenseBy(LicenseEndpointType type, string value)
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
        try
        {
#if !NETSTANDARD2_0
            return await _httpClient.GetFromJsonAsync<IEnumerable<OsiLicense?>>(query) ?? [];
#else
        var response = await _httpClient.GetAsync(query);
        return (await JsonSerializer.DeserializeAsync<IEnumerable<OsiLicense?>>(await response.Content.ReadAsStreamAsync())) ?? [];
#endif
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get license by {LicenseEndpointType} {Value}", type, value);
            return [];
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return new ValueTask(Task.CompletedTask);
    }
}