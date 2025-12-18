#if !NETSTANDARD2_0
using System.Net.Http.Json;
#endif
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Options;

namespace OpenSourceInitiative.LicenseApi.Clients;

public class OsiClient : IDisposable, IAsyncDisposable
{
    private const string LicensesEndpoint = "license";
    private const string LicenseEndpoint = "license/{0}";
    private const string NameFilter = "name={0}";
    private const string KeywordFilter = "keyword={0}";
    private const string StewardFilter = "steward={0}";
    private const string SpdxFilter = "spdx={0}";
    
    private readonly OsiClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OsiClient> _logger;
    
    public OsiClient(ILogger<OsiClient>? logger = null, OsiClientOptions? options = null, HttpClient? httpClient = null)
    {
        _logger = logger ?? NullLogger<OsiClient>.Instance;
        _options = options ?? new OsiClientOptions();
        _httpClient = httpClient?? new HttpClient { BaseAddress = _options.BaseAddress };
    }
    
#if !NETSTANDARD2_0
     public IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable() =>
        _httpClient.GetFromJsonAsAsyncEnumerable<OsiLicense>(LicensesEndpoint);
#else
    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable()
    {
        await foreach (var license in JsonSerializer.DeserializeAsyncEnumerable<OsiLicense?>(
                           await (await _httpClient.GetAsync(LicensesEndpoint)).Content.ReadAsStreamAsync()))
        {
            yield return license;
        }
    }
#endif

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return new ValueTask(Task.CompletedTask);
    }
}