using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class HttpClientExtensionsTests
{
    [Fact]
    public async Task GetLicenseTextAsync_ExtractsText_FromHtml()
    {
        var html = @"<html><body><div class='license-content'> Hello &amp; World </div></body></html>";
        using var http = new HttpClient(new StubHttpMessageHandler(_ => StubHttpMessageHandler.Html(html)));
        var lic = new OsiLicense
        {
            Links = new OsiLicenseLinks { Html = new OsiHref { Href = "https://example.test/license" } },
            Name = "X",
            Id = "x"
        };
        var text = await http.GetLicenseTextAsync(lic);
        Assert.Equal("Hello & World", text);
    }

    [Fact]
    public async Task GetLicenseTextAsync_ReturnsEmpty_WhenNoContentNode()
    {
        var html = @"<html><body><div>NO CONTENT</div></body></html>";
        using var http = new HttpClient(new StubHttpMessageHandler(_ => StubHttpMessageHandler.Html(html)));
        var lic = new OsiLicense
        {
            Links = new OsiLicenseLinks { Html = new OsiHref { Href = "https://example.test/license" } },
            Name = "X",
            Id = "x"
        };
        var text = await http.GetLicenseTextAsync(lic);
        Assert.Equal(string.Empty, text);
    }
}
