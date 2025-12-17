using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class InitializationTests
{
    [Fact]
    public async Task InitializeAsync_Is_Idempotent_And_Uses_Cache()
    {
        var calls = 0;
        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://opensource.org/api/licenses")
            {
                calls++;
                const string json =
                    "[{'id':'mit','name':'MIT','spdx_id':'MIT','_links':{'self':{'href':'s'},'html':{'href':'https://opensource.org/license/mit/'},'collection':{'href':'c'}}}]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json.Replace('\'', '"'), Encoding.UTF8, "application/json")
                };
            }

            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");

            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        await using var client = new OsiLicensesClient(new HttpClient(handler));
        await client.InitializeAsync();
        await client.InitializeAsync(); // second call should be a no-op

        // Only one base endpoint call should be necessary
        Assert.Equal(1, calls);
    }
}