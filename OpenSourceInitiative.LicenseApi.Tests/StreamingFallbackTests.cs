using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class StreamingFallbackTests
{
    private const string LicensesEndpoint = "https://opensource.org/api/licenses";

    [Fact]
    public async Task GetAllLicensesAsync_Falls_Back_When_Streaming_Fails()
    {
        var call = 0;
        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == LicensesEndpoint)
            {
                call++;
                if (call == 1)
                    // First attempt: return invalid JSON to trigger exception in streaming/primary path
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("not-json", Encoding.UTF8, "application/json")
                    };

                // Second attempt (fallback): return valid array
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

            // License HTML fetch: let it fail to cover error path for text enrichment
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        await using var client = new OsiLicensesClient(new HttpClient(handler));
        var list = await client.GetAllLicensesAsync();

        Assert.Single(list);
        Assert.Equal("MIT", list[0].SpdxId);
        // On HTML failure, LicenseText should remain empty (fail-safe)
        Assert.True(string.IsNullOrEmpty(list[0].LicenseText));
    }
}