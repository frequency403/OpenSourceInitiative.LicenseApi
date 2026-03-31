using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class OsiClientTests
{
    [Fact]
    public void Constructor_Configures_HttpClient_Headers_And_BaseAddress()
    {
        // Arrange
        var http = new HttpClient();

        // Act
        using var client = new OsiClient(NullLogger<OsiClient>.Instance, options: null, httpClient: http);

        // Assert
        http.BaseAddress!.ToString().ShouldBe("https://opensource.org/api/");
        http.DefaultRequestHeaders.Accept.ShouldContain(x => x.MediaType == "application/json");
        http.DefaultRequestHeaders.UserAgent.ShouldNotBeEmpty();
    }

    [Fact]
    public void Disposing_DefaultConstructed_Client_Disposes_Internal_HttpClient()
    {
        // Arrange
        var client = new OsiClient();

        // Act
        client.Dispose();
        var act = async () => await client.GetByOsiIdAsync("mit", TestContext.Current.CancellationToken);

        // Assert
        act.ShouldThrow<ObjectDisposedException>();
    }

    [Fact]
    public async Task Disposing_Client_Does_Not_Dispose_External_HttpClient()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var externalHttp = new HttpClient(handler);
        var client = new OsiClient(httpClient: externalHttp);

        // Act
        await client.DisposeAsync();
        var resp = await externalHttp.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/"), TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
