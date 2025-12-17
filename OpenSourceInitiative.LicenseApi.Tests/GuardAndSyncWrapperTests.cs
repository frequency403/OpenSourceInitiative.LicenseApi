using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class GuardAndSyncWrapperTests
{
    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        return new HttpClient(new StubHttpMessageHandler(responder));
    }

    [Fact]
    public async Task Guards_Return_Empty_Or_Null()
    {
        await using var client =
            new OsiLicensesClient(CreateClient(_ => StubHttpMessageHandler.Status(HttpStatusCode.NotFound)));

        var searchEmpty = await client.SearchAsync("   ");
        Assert.Empty(searchEmpty);

        var bySpdxEmpty = await client.GetBySpdxAsync("\t\n");
        Assert.Null(bySpdxEmpty);
    }

    [Fact]
    public void Sync_Wrappers_Call_Async_Under_The_Hood()
    {
        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri.StartsWith("https://opensource.org/api/licenses?name="))
            {
                const string json =
                    "[{\"id\":\"mit\",\"name\":\"MIT License\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }

            if (uri.StartsWith("https://opensource.org/api/licenses?keyword=popular-strong-community"))
            {
                const string json =
                    "[{\"id\":\"mit\",\"name\":\"MIT\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }

            if (uri.StartsWith("https://opensource.org/api/licenses?steward=eclipse-foundation"))
            {
                const string json =
                    "[{\"id\":\"epl-2.0\",\"name\":\"EPL\",\"spdx_id\":\"EPL-2.0\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/epl-2-0/\"},\"collection\":{\"href\":\"c\"}}}]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }

            if (uri.StartsWith("https://opensource.org/api/licenses?spdx=gpl*"))
            {
                const string json =
                    "[{\"id\":\"gpl-3.0\",\"name\":\"GPL\",\"spdx_id\":\"GPL-3.0-only\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/gpl-3-0/\"},\"collection\":{\"href\":\"c\"}}}]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }

            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            if (uri.Contains("/license/gpl-3-0/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>GPL3</div>");
            if (uri.Contains("/license/epl-2-0/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>EPL</div>");

            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        using var client = new OsiLicensesClient(new HttpClient(handler));

        var byName = client.GetLicensesByName("mit");
        Assert.Single(byName);

        var byKeywordEnum = client.GetLicensesByKeyword(OsiLicenseKeyword.PopularStrongCommunity);
        Assert.Single(byKeywordEnum);

        var bySteward = client.GetLicensesBySteward("eclipse-foundation");
        Assert.Single(bySteward);

        var bySpdxPattern = client.GetLicensesBySpdxPattern("gpl*");
        Assert.Single(bySpdxPattern);
    }
}