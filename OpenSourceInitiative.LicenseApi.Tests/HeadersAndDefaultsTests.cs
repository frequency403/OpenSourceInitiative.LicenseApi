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
        using var client = new OsiLicensesClient(http);

        // Assert
        http.BaseAddress!.ToString().ShouldBe("https://opensource.org/api/");
        http.DefaultRequestHeaders.Accept.ShouldHaveSingleItem();
        http.DefaultRequestHeaders.Accept.First().MediaType.ShouldBe(expectedAccept);
        http.DefaultRequestHeaders.UserAgent.ShouldHaveSingleItem();
        http.DefaultRequestHeaders.UserAgent.First().Product!.Name.ShouldBe(expectedUaName);
    }
}