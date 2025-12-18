using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Exceptions;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class FilterEndpointsTests
{
    public static IEnumerable<object[]> SuccessCases()
    {
        // name=mit
        yield return
        [
            // call
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByNameAsync("mit")),
            // first request url
            "https://opensource.org/api/licenses?name=mit",
            // json
            "[{\"id\":\"mit\",\"name\":\"MIT License\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]",
            // html map (substring -> inner text)
            new Dictionary<string, string> { ["/license/mit/"] = "MIT TEXT" },
            // expected spdx ids
            new[] { "MIT" },
            // expected text per SPDX
            new Dictionary<string, string> { ["MIT"] = "MIT TEXT" }
        ];

        // keyword=popular-strong-community (string overload) with 2 items
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByKeywordAsync("popular-strong-community")),
            "https://opensource.org/api/licenses?keyword=popular-strong-community",
            "[" + string.Join(',', new[]
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
            }) + "]",
            new Dictionary<string, string>
            {
                ["/license/mit/"] = "MIT",
                ["/license/apache-2-0/"] = "APACHE"
            },
            new[] { "MIT", "Apache-2.0" },
            new Dictionary<string, string> { ["MIT"] = "MIT", ["Apache-2.0"] = "APACHE" }
        ];

        // keyword enum overload => same request url
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity)),
            "https://opensource.org/api/licenses?keyword=popular-strong-community",
            "[{\"id\":\"mit\",\"name\":\"MIT License\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]",
            new Dictionary<string, string> { ["/license/mit/"] = "MIT" },
            new[] { "MIT" },
            new Dictionary<string, string> { ["MIT"] = "MIT" }
        ];

        // steward=eclipse-foundation
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByStewardAsync("eclipse-foundation")),
            "https://opensource.org/api/licenses?steward=eclipse-foundation",
            "[{\"id\":\"epl-2.0\",\"name\":\"Eclipse Public License 2.0\",\"spdx_id\":\"EPL-2.0\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/epl-2-0/\"},\"collection\":{\"href\":\"c\"}}}]",
            new Dictionary<string, string> { ["/license/epl-2-0/"] = "EPL" },
            new[] { "EPL-2.0" },
            new Dictionary<string, string> { ["EPL-2.0"] = "EPL" }
        ];

        // spdx=gpl* (wildcard preserved)
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesBySpdxPatternAsync("gpl*")),
            "https://opensource.org/api/licenses?spdx=gpl*",
            "[{\"id\":\"gpl-3.0\",\"name\":\"GNU GPL v3\",\"spdx_id\":\"GPL-3.0-only\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/gpl-3-0/\"},\"collection\":{\"href\":\"c\"}}}]",
            new Dictionary<string, string> { ["/license/gpl-3-0/"] = "GPL3" },
            new[] { "GPL-3.0-only" },
            new Dictionary<string, string> { ["GPL-3.0-only"] = "GPL3" }
        ];
    }

    public static IEnumerable<object[]> EdgeCases()
    {
        // Empty or whitespace => empty result (guard path) for string overloads
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByNameAsync("")),
            null!,
            null!,
            new Dictionary<string, string>(),
            0
        ];
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByStewardAsync(" \t")),
            null!,
            null!,
            new Dictionary<string, string>(),
            0
        ];
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByKeywordAsync("")),
            null!,
            null!,
            new Dictionary<string, string>(),
            0
        ];
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesBySpdxPatternAsync("")),
            null!,
            null!,
            new Dictionary<string, string>(),
            0
        ];

        // HTTP 500 => empty result
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByNameAsync("mit")),
            "https://opensource.org/api/licenses?name=mit",
            "__HTTP_500__",
            new Dictionary<string, string>(),
            0
        ];

        // Empty array => empty result
        yield return
        [
            (Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>>)(c =>
                c.GetLicensesByKeywordAsync("popular-strong-community")),
            "https://opensource.org/api/licenses?keyword=popular-strong-community",
            "[]",
            new Dictionary<string, string>(),
            0
        ];
    }

    [Theory]
    [MemberData(nameof(SuccessCases))]
    public async Task FetchFiltered_Endpoints_Success_Scenarios(
        Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>> call,
        string requestUrl,
        string json,
        Dictionary<string, string> htmlMap,
        string[] expectedSpdxIds,
        Dictionary<string, string> expectedTexts)
    {
        // Arrange
        var http = new HttpClient(new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == requestUrl)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

            foreach (var kv in htmlMap)
                if (uri.Contains(kv.Key))
                    return StubHttpMessageHandler.Html($"<div class='license-content'>{kv.Value}</div>");

            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        }));
        await using var client = new OsiLicensesClient(http);

        // Act
        var list = await call(client);

        // Assert
        list.Should().NotBeNull();
        list.Select(l => l.SpdxId).Should().BeEquivalentTo(expectedSpdxIds, o => o.WithoutStrictOrdering());
        foreach (var kv in expectedTexts) list.First(l => l.SpdxId == kv.Key).LicenseText.Should().Be(kv.Value);
    }

    [Theory]
    [MemberData(nameof(EdgeCases))]
    public async Task FetchFiltered_Endpoints_Edge_Cases(
        Func<OsiLicensesClient, Task<IReadOnlyList<OsiLicense>>> call,
        string requestUrl,
        string? json,
        Dictionary<string, string> htmlMap,
        int expectedCount)
    {
        // Arrange
        var http = new HttpClient(new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();

            // If no request URL is provided, these are guard-path cases where HTTP should not be called.
            if (requestUrl is null)
                return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);

            if (uri == requestUrl)
            {
                if (json == "__HTTP_500__") return StubHttpMessageHandler.Status(HttpStatusCode.InternalServerError);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json!, Encoding.UTF8, "application/json")
                };
            }

            foreach (var kv in htmlMap)
                if (uri.Contains(kv.Key))
                    return StubHttpMessageHandler.Html($"<div class='license-content'>{kv.Value}</div>");

            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        }));
        await using var client = new OsiLicensesClient(http);

        // Act & Assert
        if (json == "__HTTP_500__")
        {
            var act = () => call(client);
            await act.Should().ThrowAsync<OsiApiException>();
        }
        else
        {
            var list = await call(client);
            list.Should().NotBeNull();
            list.Should().HaveCount(expectedCount);
        }
    }
}