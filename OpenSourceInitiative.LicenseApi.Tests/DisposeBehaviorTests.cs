using System.Net;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class DisposeBehaviorTests
{
    [Fact]
    public void Disposing_OsiClient_Disposes_Internal_HttpClient()
    {
        // Arrange
        var client = new OsiClient();

        // Act
        client.Dispose();
        var act = () => client.GetByOsiIdAsync("mit");

        // Assert
        act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Disposing_OsiClient_Does_Not_Dispose_External_HttpClient()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var externalHttp = new HttpClient(handler);
        var client = new OsiClient(httpClient: externalHttp);

        // Act
        await client.DisposeAsync();
        var resp = await externalHttp.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/"));

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}