using System.Net;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Clients;
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
        var handler = new StubHttpMessageHandler(req => StubHttpMessageHandler.Status(HttpStatusCode.OK));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient(o => { o.PrimaryHandlerFactory = () => handler; });

        await using var provider = services.BuildServiceProvider();

        // Act: ensure typed HttpClient is created using our handler
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var http = factory.CreateClient("OsiClient");
        var response = await http.GetAsync("license", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        handler.TotalCalls.ShouldBe(1);
        // And IOsiClient is available in DI
        var client = provider.GetRequiredService<IOsiClient>();
        client.ShouldNotBeNull();
    }
    
}