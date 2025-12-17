using System.Text.Json.Serialization;

namespace OpenSourceInitiative.LicenseApi.Models;

/// <summary>
///     Link relations associated with an <see cref="OsiLicense" />.
/// </summary>
public sealed record OsiLicenseLinks
{
    /// <summary>
    ///     API endpoint for this specific license
    /// </summary>
    [JsonPropertyName("self")]
    public OsiHref Self { get; init; } = new();

    /// <summary>
    ///     Human-readable web page for this license
    /// </summary>
    [JsonPropertyName("html")]
    public OsiHref Html { get; init; } = new();

    /// <summary>
    ///     API endpoint for the licenses collection
    /// </summary>
    [JsonPropertyName("collection")]
    public OsiHref Collection { get; init; } = new();
}