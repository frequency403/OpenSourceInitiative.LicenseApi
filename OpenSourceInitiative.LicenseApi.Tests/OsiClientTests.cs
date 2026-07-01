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
        using var client = new OsiClient(NullLogger<OsiClient>.Instance, null, http);

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
        var resp = await externalHttp.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/"),
            TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("http://evil.example.com/steal")]
    [InlineData("//evil.example.com/steal")]
    [InlineData("../../admin")]
    public async Task GetByOsiIdAsync_Never_Escapes_Configured_Host(string maliciousId)
    {
        // Arrange
        Uri? requestedUri = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            requestedUri = req.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var http = new HttpClient(handler);
        using var client = new OsiClient(httpClient: http);

        // Act
        var act = async () => await client.GetByOsiIdAsync(maliciousId, TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<HttpRequestException>();
        requestedUri.ShouldNotBeNull();
        requestedUri!.Host.ShouldBe("opensource.org");
    }
}