using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class ServiceCollectionBaseAddressTests
{
    [Fact]
    public void AddOsiLicensesClient_Respects_Custom_BaseAddress()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient(o =>
        {
            o.BaseAddress = new Uri("https://unit.test/api/");
        });

        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();

        // Act
        var http = factory.CreateClient("OsiClient");

        // Assert
        http.BaseAddress!.ToString().ShouldBe("https://unit.test/api/");
    }
}