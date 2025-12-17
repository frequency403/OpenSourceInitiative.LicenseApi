using System.Text.Json.Serialization;
using OpenSourceInitiative.LicenseApi.Converter;
using OpenSourceInitiative.LicenseApi.Enums;

namespace OpenSourceInitiative.LicenseApi.Models;

/// <summary>
/// Represents a single OSI license entry as returned by the OSI License API.
/// </summary>
public sealed record OsiLicense
{
    /// <summary>
    /// OSI Unique identifier for the license
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; init; } = null!;

    /// <summary>
    /// Human readable license name, e.g. "Apache License 2.0".
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; init; } = null!;

    /// <summary>
    /// SPDX identifier, e.g. "Apache-2.0" or "MIT".
    /// </summary>
    [JsonPropertyName("spdx_id")] public string? SpdxId { get; init; }

    /// <summary>
    /// Optional license version provided by OSI.
    /// </summary>
    [JsonPropertyName("version")] public string? Version { get; init; }

    /// <summary>
    /// Date of submission to OSI (format yyyyMMdd in the payload).
    /// </summary>
    [JsonPropertyName("submission_date")]
    [JsonConverter(typeof(CustomFormatDateTimeConverter))]
    public DateTime? SubmissionDate { get; init; }

    /// <summary>
    /// URL of the submission, if provided.
    /// </summary>
    [JsonPropertyName("submission_url")] public string? SubmissionUrl { get; init; }

    /// <summary>
    /// Submitter name, if provided.
    /// </summary>
    [JsonPropertyName("submitter_name")] public string? SubmitterName { get; init; }

    /// <summary>
    /// Indicates whether the license is approved by OSI.
    /// </summary>
    [JsonPropertyName("approved")] public bool Approved { get; init; }

    /// <summary>
    /// Date of approval by OSI (format yyyyMMdd in the payload).
    /// </summary>
    [JsonPropertyName("approval_date")]
    [JsonConverter(typeof(CustomFormatDateTimeConverter))]
    public DateTime? ApprovalDate { get; init; }

    /// <summary>
    /// Version value provided by the license steward, if any.
    /// </summary>
    [JsonPropertyName("license_steward_version")]
    public string? LicenseStewardVersion { get; init; }

    /// <summary>
    /// URL provided by the license steward, if any.
    /// </summary>
    [JsonPropertyName("license_steward_url")]
    public string? LicenseStewardUrl { get; init; }

    /// <summary>
    /// Board minutes URL or reference, if any.
    /// </summary>
    [JsonPropertyName("board_minutes")] public string? BoardMinutes { get; init; }

    /// <summary>
    /// List of stewards involved, if any.
    /// </summary>
    [JsonPropertyName("stewards")] public List<string> Stewards { get; init; } = [];

    /// <summary>
    /// Classification keywords associated with the license as defined by OSI.
    /// </summary>
    /// <remarks>
    /// Values are mapped from the OSI API string tokens (e.g., "popular-strong-community").
    /// </remarks>
    [JsonPropertyName("keywords")]
    [JsonConverter(typeof(OsiLicenseKeywordsConverter))]
    public List<OsiLicenseKeyword> Keywords { get; init; } = [];

    /// <summary>
    /// Links to the API self page, public HTML page and collection.
    /// </summary>
    [JsonPropertyName("_links")] public OsiLicenseLinks Links { get; init; } = new();

    /// <summary>
    /// Extracted, human-readable license text from the license HTML page.
    /// </summary>
    [JsonIgnore] public string LicenseText { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => Name;
}