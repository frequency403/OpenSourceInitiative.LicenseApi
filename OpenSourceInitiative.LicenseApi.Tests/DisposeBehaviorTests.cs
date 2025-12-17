using System.Net;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class DisposeBehaviorTests
{
    [Fact]
    public void Disposing_DefaultConstructed_Client_Disposes_Internal_HttpClient()
    {
        var client = new OsiLicensesClient();
        client.Dispose();

        // After dispose, any operation using the internal HttpClient should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => client.Initialize());
    }

    [Fact]
    public async Task Disposing_Client_Does_Not_Dispose_External_HttpClient()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var externalHttp = new HttpClient(handler);

        var client = new OsiLicensesClient(externalHttp);
        await client.DisposeAsync();

        // External HttpClient should remain usable
        var resp = await externalHttp.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/"));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
