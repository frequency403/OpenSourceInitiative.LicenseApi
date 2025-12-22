#if !NETSTANDARD2_0
using System.Net.Http.Json;
#else
using System.Text.Json;
#endif
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSourceInitiative.LicenseApi.Converter;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Options;

namespace OpenSourceInitiative.LicenseApi.Clients;

public class OsiClient : IOsiClient
{
    private const string AllLicensesEndpoint = "license";
    private const string SingleLicenseEndpoint = "license/{0}";
    private const string NameFilter = "name={0}";
    private const string KeywordFilter = "keyword={0}";
    private const string StewardFilter = "steward={0}";
    private const string SpdxFilter = "spdx={0}";

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly OsiClientOptions _options;
    private readonly ILogger<OsiClient> _logger;

    public OsiClient(
        ILogger<OsiClient>? logger = null,
        OsiClientOptions? options = null,
        HttpClient? httpClient = null)
    {
        _options = options ?? new OsiClientOptions();
        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient == null;
        _logger = logger ?? NullLogger<OsiClient>.Instance;

        ConfigureHttpClient();
    }

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
    public Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword) => GetLicenseBy(LicenseEndpointType.Keyword, OsiLicenseKeywordMapping.ToApiValue(keyword));
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

#if !NETSTANDARD2_0
        return await _httpClient.GetFromJsonAsync<IEnumerable<OsiLicense?>>(query) ?? [];
#else
        var response = await _httpClient.GetAsync(query);
        response.EnsureSuccessStatusCode();
        return (await JsonSerializer.DeserializeAsync<IEnumerable<OsiLicense?>>(await response.Content.ReadAsStreamAsync())) ?? [];
#endif
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

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress ??= _options.BaseAddress;

        if (_httpClient.DefaultRequestHeaders.Accept.Count == 0)
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OpenSourceInitiative-LicenseApi-Client", "1.0"));
    }
}