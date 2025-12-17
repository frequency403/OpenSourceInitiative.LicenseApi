using System.Net;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class ServiceCollectionBaseAddressTests
{
    [Fact]
    public async Task AddOsiLicensesClient_Respects_Custom_BaseAddress()
    {
        // Arrange
        string? firstUri = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            firstUri ??= req.RequestUri!.ToString();
            // Minimal OK response for any request
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            };
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient(o =>
        {
            o.BaseAddress = new Uri("https://unit.test/api/");
            o.PrimaryHandlerFactory = () => handler;
        });

        await using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IOsiLicensesClient>();

        // Act
        var _ = await client.GetAllLicensesAsync();

        // Assert
        firstUri.Should().Be("https://unit.test/api/licenses");
    }
}