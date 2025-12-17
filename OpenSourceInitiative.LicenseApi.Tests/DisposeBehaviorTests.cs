using System.Net;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class DisposeBehaviorTests
{
    [Fact]
    public void Disposing_DefaultConstructed_Client_Disposes_Internal_HttpClient()
    {
        // Arrange
        var client = new OsiLicensesClient();

        // Act
        client.Dispose();
        var act = () => client.Initialize();

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task Disposing_Client_Does_Not_Dispose_External_HttpClient()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var externalHttp = new HttpClient(handler);
        var client = new OsiLicensesClient(externalHttp);

        // Act
        await client.DisposeAsync();
        var resp = await externalHttp.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/"));

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}