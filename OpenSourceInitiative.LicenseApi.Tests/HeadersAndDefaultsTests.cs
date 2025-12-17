using System.Linq;
using System.Net.Http.Headers;
using OpenSourceInitiative.LicenseApi.Clients;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class HeadersAndDefaultsTests
{
    [Fact]
    public void Constructor_Sets_Default_BaseAddress_And_Headers()
    {
        var http = new HttpClient();

        // No base address and no default headers pre-configured
        Assert.Null(http.BaseAddress);
        Assert.Empty(http.DefaultRequestHeaders.Accept);
        Assert.Empty(http.DefaultRequestHeaders.UserAgent);

        // Creating the client wires defaults into the provided HttpClient
        using var client = new OsiLicensesClient(http);

        Assert.Equal("https://opensource.org/api/", http.BaseAddress!.ToString());
        Assert.Contains(http.DefaultRequestHeaders.Accept, m => m.MediaType == "application/json");
        Assert.Contains(http.DefaultRequestHeaders.UserAgent, p => p.Product?.Name == "OpenSourceInitiative-LicenseApi-Client");
    }

    [Fact]
    public void Constructor_DoesNot_Duplicate_Headers_When_Present()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CustomAgent", "1.0"));

        using var client = new OsiLicensesClient(http);

        // Accept header remains as provided (no duplicate application/json added)
        Assert.Single(http.DefaultRequestHeaders.Accept);
        Assert.Equal("text/plain", http.DefaultRequestHeaders.Accept.First().MediaType);

        // UserAgent remains as provided (no default added)
        Assert.Single(http.DefaultRequestHeaders.UserAgent);
        Assert.Equal("CustomAgent", http.DefaultRequestHeaders.UserAgent.First().Product!.Name);
    }
}
