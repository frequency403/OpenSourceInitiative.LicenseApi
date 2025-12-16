using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class SearchAndLookupTests
{
    private const string ApiBase = "https://opensource.org/api/licenses";

    private static (HttpClient client, StubHttpMessageHandler handler) CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler);
        return (http, handler);
    }

    [Fact]
    public async Task SearchAsync_ByNameAndId_ReturnsMatches()
    {
        var json = "[" + string.Join(',', new[]
        {
            "{\"id\":\"mit\",\"name\":\"MIT License\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}",
            "{\"id\":\"apache-2.0\",\"name\":\"Apache License 2.0\",\"spdx_id\":\"Apache-2.0\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/apache-2-0/\"},\"collection\":{\"href\":\"c\"}}}"
        }) + "]";

        var (http, _) = CreateClient(req =>
        {
            if (req.RequestUri!.ToString() == ApiBase)
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            if (req.RequestUri!.ToString().Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            if (req.RequestUri!.ToString().Contains("/license/apache-2-0/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>APACHE</div>");
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);
        var all = await client.GetAllLicensesAsync();
        Assert.Equal(2, all.Count);

        var byName = await client.SearchAsync("Apache");
        Assert.Single(byName);
        Assert.Equal("Apache-2.0", byName[0].SpdxId);

        var byId = await client.SearchAsync("mit");
        Assert.Single(byId);
        Assert.Equal("MIT", byId[0].SpdxId);

        // sync wrappers
        var byNameSync = client.Search("Apache");
        Assert.Single(byNameSync);
        var allSync = client.GetAllLicenses();
        Assert.Equal(2, allSync.Count);
    }

    [Fact]
    public async Task GetBySpdx_Works_Async_And_Sync()
    {
        var json = "[{\"id\":\"mit\",\"name\":\"MIT License\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]";
        var (http, _) = CreateClient(req =>
        {
            if (req.RequestUri!.ToString() == ApiBase)
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            if (req.RequestUri!.ToString().Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);
        var licAsync = await client.GetBySpdxAsync("MIT");
        Assert.NotNull(licAsync);
        Assert.Equal("MIT", licAsync!.SpdxId);

        var licSync = client.GetBySpdx("MIT");
        Assert.NotNull(licSync);
        Assert.Equal("MIT", licSync!.SpdxId);
    }
}
