using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class ServiceCollectionHeadersTests
{
    [Fact]
    public async Task AddOsiLicensesClient_Sends_Default_Accept_And_UserAgent_Headers()
    {
        ProductInfoHeaderValue[]? capturedUa = null;
        MediaTypeWithQualityHeaderValue[]? capturedAccept = null;
        string? firstApiUri = null;

        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            // Capture headers and the first API call to the licenses endpoint
            if (firstApiUri is null && uri == "https://opensource.org/api/licenses")
            {
                firstApiUri = uri;
                capturedAccept = req.Headers.Accept.ToArray();
                capturedUa = req.Headers.UserAgent.ToArray();

                const string json = "[{'id':'mit','name':'MIT','spdx_id':'MIT','_links':{'self':{'href':'s'},'html':{'href':'https://opensource.org/license/mit/'},'collection':{'href':'c'}}}]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json.Replace('\'', '"'), Encoding.UTF8, "application/json")
                };
            }

            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");

            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient(o => o.PrimaryHandlerFactory = () => handler);

        await using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IOsiLicensesClient>();

        var licenses = await client.GetAllLicensesAsync();
        Assert.Single(licenses);

        Assert.Equal("https://opensource.org/api/licenses", firstApiUri);
        Assert.Contains(capturedAccept!, m => m.MediaType == "application/json");
        Assert.Contains(capturedUa!, p => p.Product?.Name == "OpenSourceInitiative-LicenseApi-Client");
    }
}
