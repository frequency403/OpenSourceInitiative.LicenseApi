using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class ServiceCollectionHeadersTests
{
    [Fact]
    public void AddOsiLicensesClient_Configures_Default_Accept_And_UserAgent_Headers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient();

        // Act
        using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var http = factory.CreateClient("OsiClient");

        // Assert
        http.DefaultRequestHeaders.Accept.ShouldContain(x => x.MediaType == "application/json");
        http.DefaultRequestHeaders.UserAgent.ShouldContain(x => x.Product != null && x.Product.Name == "OpenSourceInitiative.LicenseApi");
        http.BaseAddress!.ToString().ShouldBe("https://opensource.org/api/");
    }
}