using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class SearchAndLookupTests
{
    private const string ApiBase = "https://opensource.org/api/licenses";

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new HttpClient(new StubHttpMessageHandler(responder));
    }

    [Theory]
    [InlineData("Apache", "Apache-2.0")]
    [InlineData("mit", "MIT")]
    public async Task SearchAsync_Finds_By_Name_Or_Id_And_Sync_Wrapper_Works(string query, string expectedSpdx)
    {
        // Arrange
        var json = "[" + string.Join(',', new[]
        {
            "{\"id\":\"mit\",\"name\":\"MIT License\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}",
            "{\"id\":\"apache-2.0\",\"name\":\"Apache License 2.0\",\"spdx_id\":\"Apache-2.0\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/apache-2-0/\"},\"collection\":{\"href\":\"c\"}}}"
        }) + "]";

        var http = CreateClient(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == ApiBase)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            if (uri.Contains("/license/apache-2-0/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>APACHE</div>");
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);

        // Act
        var all = await client.GetAllLicensesAsync();
        var results = await client.SearchAsync(query);
        var syncResults = client.Search(query); // sync wrapper
        var allSync = client.GetAllLicenses(); // sync wrapper for GetAll

        // Assert
        all.Count.Should().Be(2);
        results.Should().ContainSingle().Which.SpdxId.Should().Be(expectedSpdx);
        syncResults.Should().ContainSingle().Which.SpdxId.Should().Be(expectedSpdx);
        allSync.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetBySpdx_Works_Async_And_Sync()
    {
        // Arrange
        var json =
            "[{\"id\":\"mit\",\"name\":\"MIT License\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]";
        var http = CreateClient(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == ApiBase)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);

        // Act
        var licAsync = await client.GetBySpdxAsync("MIT");
        var licSync = client.GetBySpdx("MIT"); // sync variant

        // Assert
        licAsync.Should().NotBeNull();
        licAsync!.SpdxId.Should().Be("MIT");
        licSync.Should().NotBeNull();
        licSync!.SpdxId.Should().Be("MIT");
    }
}