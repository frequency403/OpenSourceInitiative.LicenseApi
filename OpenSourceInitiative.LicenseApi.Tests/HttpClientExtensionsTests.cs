using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class HttpClientExtensionsTests
{
    [Theory]
    [InlineData("<html><body><div class='license-content'> Hello &amp; World </div></body></html>", "Hello & World")]
    [InlineData("<html><body><div>NO CONTENT</div></body></html>", "")]
    public async Task GetLicenseTextAsync_Parses_Text_Or_Empty(string html, string expected)
    {
        // Arrange
        using var http = new HttpClient(new StubHttpMessageHandler(_ => StubHttpMessageHandler.Html(html)));
        var lic = new OsiLicense
        {
            Links = new OsiLicenseLinks { Html = new OsiHref { Href = "https://example.test/license" } },
            Name = "X",
            Id = "x"
        };

        // Act
        var text = await http.GetLicenseTextAsync(lic);

        // Assert
        text.Should().Be(expected);
    }
}