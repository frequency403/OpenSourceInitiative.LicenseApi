using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class FilterEndpointsTests
{
    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new HttpClient(new StubHttpMessageHandler(responder));
    }

    [Fact]
    public async Task GetLicensesByNameAsync_Calls_FilterEndpoint_And_Parses()
    {
        var http = CreateClient(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://opensource.org/api/licenses?name=mit")
            {
                var json = "[" + string.Join(',', new[]
                {
                    "{" + string.Join(',',
                        "\"id\":\"mit\"",
                        "\"name\":\"MIT License\"",
                        "\"spdx_id\":\"MIT\"",
                        "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}"
                    ) + "}"
                }) + "]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT TEXT</div>");
            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);
        var list = await client.GetLicensesByNameAsync("mit");
        Assert.Single(list);
        Assert.Equal("MIT", list[0].SpdxId);
        Assert.Equal("MIT TEXT", list[0].LicenseText);
    }

    [Fact]
    public async Task GetLicensesByKeywordAsync_Parses()
    {
        var http = CreateClient(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://opensource.org/api/licenses?keyword=popular-strong-community")
            {
                var json = "[" + string.Join(',', new[]
                {
                    "{" + string.Join(',',
                        "\"id\":\"mit\"",
                        "\"name\":\"MIT License\"",
                        "\"spdx_id\":\"MIT\"",
                        "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}"
                    ) + "}",
                    "{" + string.Join(',',
                        "\"id\":\"apache-2.0\"",
                        "\"name\":\"Apache License 2.0\"",
                        "\"spdx_id\":\"Apache-2.0\"",
                        "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/apache-2-0/\"},\"collection\":{\"href\":\"c\"}}"
                    ) + "}"
                }) + "]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            if (uri.Contains("/license/apache-2-0/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>APACHE</div>");
            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);
        var list = await client.GetLicensesByKeywordAsync("popular-strong-community");
        Assert.Equal(2, list.Count);
        Assert.Contains(list, l => l.SpdxId == "MIT");
        Assert.Contains(list, l => l.SpdxId == "Apache-2.0");
    }

    [Fact]
    public async Task GetLicensesByKeywordAsync_Enum_Overload_Builds_Correct_Query()
    {
        var http = CreateClient(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://opensource.org/api/licenses?keyword=popular-strong-community")
            {
                var json = "[" + string.Join(',', new[]
                {
                    "{" + string.Join(',',
                        "\"id\":\"mit\"",
                        "\"name\":\"MIT License\"",
                        "\"spdx_id\":\"MIT\"",
                        "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}"
                    ) + "}"
                }) + "]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);
        var list = await client.GetLicensesByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity);
        Assert.Single(list);
        Assert.Equal("MIT", list[0].SpdxId);
    }

    [Fact]
    public async Task GetLicensesByStewardAsync_Parses()
    {
        var http = CreateClient(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://opensource.org/api/licenses?steward=eclipse-foundation")
            {
                var json = "[" + string.Join(',', new[]
                {
                    "{" + string.Join(',',
                        "\"id\":\"epl-2.0\"",
                        "\"name\":\"Eclipse Public License 2.0\"",
                        "\"spdx_id\":\"EPL-2.0\"",
                        "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/epl-2-0/\"},\"collection\":{\"href\":\"c\"}}"
                    ) + "}"
                }) + "]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            if (uri.Contains("/license/epl-2-0/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>EPL</div>");
            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);
        var list = await client.GetLicensesByStewardAsync("eclipse-foundation");
        Assert.Single(list);
        Assert.Equal("EPL-2.0", list[0].SpdxId);
        Assert.Equal("EPL", list[0].LicenseText);
    }

    [Fact]
    public async Task GetLicensesBySpdxPatternAsync_Parses_Wildcard()
    {
        var http = CreateClient(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://opensource.org/api/licenses?spdx=gpl*")
            {
                var json = "[" + string.Join(',', new[]
                {
                    "{" + string.Join(',',
                        "\"id\":\"gpl-3.0\"",
                        "\"name\":\"GNU GPL v3\"",
                        "\"spdx_id\":\"GPL-3.0-only\"",
                        "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/gpl-3-0/\"},\"collection\":{\"href\":\"c\"}}"
                    ) + "}"
                }) + "]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            if (uri.Contains("/license/gpl-3-0/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>GPL3</div>");
            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(http);
        var list = await client.GetLicensesBySpdxPatternAsync("gpl*");
        Assert.Single(list);
        Assert.Equal("GPL-3.0-only", list[0].SpdxId);
        Assert.Equal("GPL3", list[0].LicenseText);
    }
}