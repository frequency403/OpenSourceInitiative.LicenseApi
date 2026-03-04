using System.Net.Http.Headers;
using OpenSourceInitiative.LicenseApi.Clients;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class HeadersAndDefaultsTests
{
    [Theory]
    [InlineData(false, "application/json", "OpenSourceInitiative.LicenseApi")] // library default UA is assembly name
    [InlineData(true, "text/plain", "CustomAgent")] // pre-set headers -> additional defaults are appended
    public void Constructor_Sets_Defaults_And_Appends_If_Preset(bool prePopulate, string expectedAccept, string expectedUaName)
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
        http.BaseAddress!.ToString().ShouldBe("https://opensource.org/api/");
        if (!prePopulate)
        {
            http.DefaultRequestHeaders.Accept.ShouldHaveSingleItem();
            http.DefaultRequestHeaders.Accept.First().MediaType.ShouldBe(expectedAccept);
            http.DefaultRequestHeaders.UserAgent.ShouldContain(x => x.Product!.Name == expectedUaName);
        }
        else
        {
            // Accept should now contain both the pre-populated and the library default
            http.DefaultRequestHeaders.Accept.Count.ShouldBeGreaterThanOrEqualTo(2);
            http.DefaultRequestHeaders.Accept.ShouldContain(x => x.MediaType == expectedAccept);
            http.DefaultRequestHeaders.Accept.ShouldContain(x => x.MediaType == "application/json");

            // UA should include both the custom and the default
            http.DefaultRequestHeaders.UserAgent.ShouldContain(x => x.Product!.Name == expectedUaName);
            http.DefaultRequestHeaders.UserAgent.ShouldContain(x => x.Product!.Name == "OpenSourceInitiative.LicenseApi");
        }
    }
}