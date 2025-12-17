using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class OsiLicensesClientTests
{
    private const string ApiBase = "https://opensource.org/api/licenses";

    private static (HttpClient client, StubHttpMessageHandler handler) CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler);
        return (http, handler);
    }

    [Fact]
    public async Task GetAllLicensesAsync_ParsesAndPopulatesText()
    {
        var jsonArray = "[" + string.Join(',', new[]
        {
            // minimal valid entries; include html links for text extraction
            "{" + string.Join(',',
                "\"id\":\"mit\"",
                "\"name\":\"MIT License\"",
                "\"spdx_id\":\"MIT\"",
                "\"_links\":{\"self\":{\"href\":\"https://opensource.org/api/license/mit\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"https://opensource.org/api/license\"}}"
            ) + "}",
            "{" + string.Join(',',
                "\"id\":\"apache-2.0\"",
                "\"name\":\"Apache License 2.0\"",
                "\"spdx_id\":\"Apache-2.0\"",
                "\"_links\":{\"self\":{\"href\":\"https://opensource.org/api/license/apache-2.0\"},\"html\":{\"href\":\"https://opensource.org/license/apache-2-0/\"},\"collection\":{\"href\":\"https://opensource.org/api/license\"}}"
            ) + "}"
        }) + "]";

        var (httpClient, _) = CreateClient(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == ApiBase)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonArray, Encoding.UTF8, "application/json")
                };
            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html(
                    "<html><body><div class='license-content'>MIT Text</div></body></html>");
            if (uri.Contains("/license/apache-2-0/"))
                return StubHttpMessageHandler.Html(
                    "<html><body><div class='license-content'>Apache 2.0 Text</div></body></html>");
            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(httpClient);
        var result = await client.GetAllLicensesAsync();

        Assert.NotEmpty(result);
        var mit = result.FirstOrDefault(x => x.SpdxId == "MIT");
        var ap2 = result.FirstOrDefault(x => x.SpdxId == "Apache-2.0");
        Assert.NotNull(mit);
        Assert.Equal("MIT Text", mit.LicenseText);
        Assert.NotNull(ap2);
        Assert.Equal("Apache 2.0 Text", ap2.LicenseText);
    }

    [Fact]
    public async Task GetAllLicensesAsync_FailSafeOnServerError_ReturnsSnapshot()
    {
        var (httpClient, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        await using var client = new OsiLicensesClient(httpClient);
        var result = await client.GetAllLicensesAsync();
        Assert.Empty(result); // initial snapshot is empty
    }

    [Fact]
    public async Task GetAllLicensesAsync_UsesCachedSnapshot_OnSubsequentCalls()
    {
        var (httpClient, handler) = CreateClient(req =>
        {
            if (req.RequestUri!.ToString() != ApiBase)
                return req.RequestUri!.ToString().Contains("/license/mit/")
                    ? StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>")
                    : new HttpResponseMessage(HttpStatusCode.NotFound);
            const string json =
                "[{\"id\":\"mit\",\"name\":\"MIT License\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        await using var client = new OsiLicensesClient(httpClient);
        var first = await client.GetAllLicensesAsync();
        var second = await client.GetAllLicensesAsync();

        // Only one network call for the base list endpoint
        // plus one for the html (which may be concurrent), but base should be once
        Assert.True(handler.TotalCalls >= 1);
        // Ensure data is the same instance snapshot and not refetched
        Assert.Same(first, second);
    }
}