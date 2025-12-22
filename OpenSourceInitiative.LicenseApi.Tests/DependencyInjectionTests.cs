using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public async Task AddOsiLicensesClient_RegistersTypedClient_AndWorksWithCustomHandler()
    {
        // Arrange
        const string json =
            "[{\"id\":\"mit\",\"name\":\"MIT\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"https://opensource.org/license/mit/\"},\"collection\":{\"href\":\"c\"}}}]";
        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri == "https://opensource.org/api/license")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");
            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient(o => { o.PrimaryHandlerFactory = () => handler; });

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IOsiLicensesClient>();

        // Act
        var licenses = await client.GetAllLicensesAsync();

        // Assert
        licenses.Should().ContainSingle();
        licenses[0].SpdxId.Should().Be("MIT");
        //licenses[0].LicenseText.Should().Be("MIT");
    }

    [Fact]
    public void AddOsiLicensesClient_RegistersIOsiClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient();

        using var provider = services.BuildServiceProvider();
        
        // Act
        var osiClient = provider.GetService<IOsiClient>();

        // Assert
        osiClient.Should().NotBeNull();
    }
}