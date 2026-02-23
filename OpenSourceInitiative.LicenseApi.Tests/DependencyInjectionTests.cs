using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

using OpenSourceInitiative.LicenseApi.Clients;

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

    [Fact]
    public void AddOsiLicensesClient_Resolves_Caching_Client_By_Default()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient();

        using var sp = services.BuildServiceProvider();

        // Act
        var client = sp.GetRequiredService<IOsiClient>();

        // Assert
        client.ShouldBeOfType<OsiCachingClient>();
    }

    [Fact]
    public void AddOsiLicensesClient_Resolves_NonCaching_Client_When_Disabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient(o => o.EnableCaching = false);

        using var sp = services.BuildServiceProvider();

        // Act
        var client = sp.GetRequiredService<IOsiClient>();

        // Assert
        client.ShouldBeOfType<OsiClient>();
    }

    [Fact]
    public void AddOsiLicensesClient_Registers_Keyed_NonCaching_Client_When_Caching_Enabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient();

        using var sp = services.BuildServiceProvider();

        // Act
        var nonCaching = sp.GetRequiredKeyedService<IOsiClient>("OsiNonCachingClient");

        // Assert
        nonCaching.ShouldBeOfType<OsiClient>();
    }
}