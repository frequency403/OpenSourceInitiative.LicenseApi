using System.Text.Json.Serialization;

namespace OpenSourceInitiative.LicenseApi.Models;

/// <summary>
/// Minimal href wrapper used by OSI API link objects.
/// </summary>
public sealed record OsiHref
{
    /// <summary>
    /// The absolute link value.
    /// </summary>
    [JsonPropertyName("href")]
    public string Href { get; init; } = null!;
}