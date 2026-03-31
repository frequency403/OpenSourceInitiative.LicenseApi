using System.Net;
using System.Net.Http.Headers;
#if !NETSTANDARD2_0
using System.Net.Mime;
#endif
using HtmlAgilityPack;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Options;

namespace OpenSourceInitiative.LicenseApi.Extensions;

/// <summary>
///     Helper extensions for <see cref="HttpClient" /> used by the OSI client.
/// </summary>
internal static class HttpClientExtensions
{
    private const string ApplicationJsonMediaType =
#if !NETSTANDARD2_0
            MediaTypeNames.Application.Json
#else
            "application/json"
#endif
        ;
    
    private const string ClassNameContainingLicenseText = "license-content";

    /// <param name="client">The HTTP client used to perform the GET request.</param>
    extension(HttpClient client)
    {
        /// <summary>
        ///     Downloads and extracts the human-readable license text from the license HTML page.
        /// </summary>
        /// <param name="license">The license whose <see cref="OsiLicenseLinks.Html" /> link is used.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Plain text content of the HTML node with class 'license-content', or an empty string if not found.</returns>
        internal async Task<string> GetLicenseTextAsync(OsiLicense license,
            CancellationToken cancellationToken = default)
        {
            var response = await client.GetAsync(license.Links.Html.Href, cancellationToken);
            if((int)response.StatusCode is 301) // Int cast here, enum has moved and moved permanently which causes pattern matching to fail
                response = await client.GetAsync(new Uri(new Uri("https://opensource.org"), response.Headers.Location), cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to fetch license text for {license}: {response.ReasonPhrase}");

            using var stream = await response.Content.ReadAsStreamAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(stream);
            return HtmlEntity.DeEntitize(htmlDocument.DocumentNode
                                             .Descendants().FirstOrDefault(n => n.HasClass(ClassNameContainingLicenseText))?.InnerText ??
                                         string.Empty)
                .Trim();
        }

        internal void ConfigureForLicenseApi(OsiClientOptions options)
        {
            client.BaseAddress ??= options.BaseAddress;
            if (MediaTypeWithQualityHeaderValue.TryParse(ApplicationJsonMediaType, out var headerValue))
                client.DefaultRequestHeaders.Accept.Add(headerValue);

            foreach (var productInfoHeaderValue in options.UserAgent)
            {
                client.DefaultRequestHeaders.UserAgent.Add(productInfoHeaderValue);
            }
        }
    }
}