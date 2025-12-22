using System.Net.Http.Headers;
using OpenSourceInitiative.LicenseApi.Clients;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class HeadersAndDefaultsTests
{
    [Theory]
    [InlineData(false, "application/json", "OpenSourceInitiative-LicenseApi-Client")] // no pre-set -> defaults added
    [InlineData(true, "text/plain", "CustomAgent")] // pre-set headers -> no duplicates
    public void Constructor_Sets_Defaults_Idempotently(bool prePopulate, string expectedAccept, string expectedUaName)
    {
        // Arrange
        var http = new HttpClient();
        if (prePopulate)
        {
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CustomAgent", "1.0"));
        }

        // Act
        using var client = new OsiClient(httpClient: http);

        // Assert
        http.BaseAddress!.ToString().Should().Be("https://opensource.org/api/");
        http.DefaultRequestHeaders.Accept.Should().ContainSingle();
        http.DefaultRequestHeaders.Accept.First().MediaType.Should().Be(expectedAccept);
        http.DefaultRequestHeaders.UserAgent.Should().ContainSingle();
        http.DefaultRequestHeaders.UserAgent.First().Product!.Name.Should().Be(expectedUaName);
    }
}