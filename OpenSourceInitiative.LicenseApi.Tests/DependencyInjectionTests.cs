using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.DependencyInjection.Extensions;
using OpenSourceInitiative.LicenseApi.DependencyInjection.Options;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public async Task AddOsiLicensesClient_RegistersTypedClient_AndWorksWithCustomHandler()
    {
        var json = "[{\"id\":\"mit\",\"name\":\"MIT\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]";
        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://opensource.org/api/licenses")
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            return StubHttpMessageHandler.Status(System.Net.HttpStatusCode.NotFound);
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient(o =>
        {
            o.EnableLogging = true;
            o.PrimaryHandlerFactory = () => handler;
        });

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IOsiLicensesClient>();

        var licenses = await client.GetAllLicensesAsync();
        Assert.Single(licenses);
        Assert.Equal("MIT", licenses[0].SpdxId);
        Assert.Equal("MIT", licenses[0].LicenseText);
    }
}
