using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Exceptions;
using OpenSourceInitiative.LicenseApi.Options;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class OsiLicensesClientTests
{
    private const string ApiBase = "https://opensource.org/api/license";

    private static (IOsiClient osiClient, StubHttpMessageHandler handler) CreateOsiClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler);
        return (new OsiClient(httpClient: http), handler);
    }

    [Fact]
    public async Task GetAllLicensesAsync_ParsesAndPopulatesText()
    {
        // Arrange
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

        var (osiClient, _) = CreateOsiClient(req =>
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

        await using var client = new OsiLicensesClient(osiClient);

        // Act
        var result = await client.GetAllLicensesAsync();

        // Assert
        result.Should().NotBeEmpty();
        var mit = result.FirstOrDefault(x => x.SpdxId == "MIT");
        var ap2 = result.FirstOrDefault(x => x.SpdxId == "Apache-2.0");
        mit.Should().NotBeNull();
        mit!.LicenseText.Should().Be("MIT Text");
        ap2.Should().NotBeNull();
        ap2!.LicenseText.Should().Be("Apache 2.0 Text");
    }

    [Fact]
    public async Task GetAllLicensesAsync_UsesCachedSnapshot_OnSubsequentCalls()
    {
        // Arrange
        var (baseClient, handler) = CreateOsiClient(req =>
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

        var cachingClient = new OsiCachingClient(baseClient);
        await using var client = new OsiLicensesClient(cachingClient);

        // Act
        var first = await client.GetAllLicensesAsync();
        var second = await client.GetAllLicensesAsync();

        // Assert
        handler.TotalCalls.Should().Be(2);
        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task GetAllLicensesAsync_Refetches_WhenNoCaching()
    {
        // Arrange
        var (baseClient, handler) = CreateOsiClient(req =>
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

        await using var client = new OsiLicensesClient(baseClient);

        // Act
        var first = await client.GetAllLicensesAsync();
        var second = await client.GetAllLicensesAsync();

        // Assert
        handler.TotalCalls.Should().Be(4);
        second.Should().BeEquivalentTo(first);
        second.Should().NotBeSameAs(first);
    }
}