using System.Net.Http.Headers;
using HtmlAgilityPack;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Options;
#if !NETSTANDARD2_0
using System.Net.Mime;
#endif

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
        /// Because the License API does not provide a license text, we need to fetch it ourselfes.
        /// </summary>
        /// <param name="license"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal async Task<string> GetLicenseTextAsync(OsiLicense license,
            CancellationToken cancellationToken = default)
        {
            var stewardUrl = license.LicenseStewardUrl;
            var hasStewardUrl = !string.IsNullOrWhiteSpace(stewardUrl);
            
            var osiText = await client.TryFetchHtmlLicenseTextAsync(license.Links.Html.Href, cancellationToken);
            if (!string.IsNullOrWhiteSpace(osiText))
                return osiText!;

            // Step 3: Steward HTML — last resort if OSI page fails or yields no content
            if (hasStewardUrl && !stewardUrl!.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                var stewardText = await client.TryFetchHtmlLicenseTextAsync(stewardUrl, cancellationToken);
                if (!string.IsNullOrWhiteSpace(stewardText))
                    return stewardText!;
            }

            return string.Empty;
        }

        /// <summary>
        ///     Fetches and parses an HTML page, extracting the inner text of the node
        ///     matching <see cref="ClassNameContainingLicenseText"/>.
        /// </summary>
        /// <param name="url">The URL of the HTML page to fetch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        ///     The de-entitized, trimmed inner text of the license node,
        ///     or <see langword="null"/> if the request fails or the node is not found.
        /// </returns>
        private async Task<string?> TryFetchHtmlLicenseTextAsync(string url,
            CancellationToken cancellationToken)
        {
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

#if !NETSTANDARD2_0
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
    using var stream = await response.Content.ReadAsStreamAsync();
#endif

            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(stream);

            var node = htmlDocument.DocumentNode
                .SelectSingleNode(
                    $"//*[contains(concat(' ', @class, ' '), ' {ClassNameContainingLicenseText} ')]");

            var text = HtmlEntity.DeEntitize(node?.InnerText ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        internal void ConfigureForLicenseApi(OsiClientOptions options)
        {
            client.BaseAddress ??= options.BaseAddress;
            if (MediaTypeWithQualityHeaderValue.TryParse(ApplicationJsonMediaType, out var headerValue))
                client.DefaultRequestHeaders.Accept.Add(headerValue);

            foreach (var productInfoHeaderValue in options.UserAgent)
                client.DefaultRequestHeaders.UserAgent.Add(productInfoHeaderValue);
        }
    }
}