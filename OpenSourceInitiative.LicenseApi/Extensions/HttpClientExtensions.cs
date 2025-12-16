using HtmlAgilityPack;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Extensions;

/// <summary>
/// Helper extensions for <see cref="HttpClient"/> used by the OSI client.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Downloads and extracts the human-readable license text from the license HTML page.
    /// </summary>
    /// <param name="client">The HTTP client used to perform the GET request.</param>
    /// <param name="license">The license whose <see cref="OsiLicenseLinks.Html"/> link is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Plain text content of the HTML node with class 'license-content', or an empty string if not found.</returns>
    public static async Task<string> GetLicenseTextAsync(this HttpClient client, OsiLicense license,
        CancellationToken cancellationToken = default)
    {
        var stream = await client.GetStreamAsync(license.Links.Html.Href
#if NET10_0_OR_GREATER
            , cancellationToken
#endif
        );

        var htmlDocument = new HtmlDocument();
        htmlDocument.Load(stream);
        return HtmlEntity.DeEntitize(htmlDocument.DocumentNode
            .Descendants().FirstOrDefault(n => n.HasClass("license-content"))?.InnerText ?? string.Empty).Trim();
    }
}