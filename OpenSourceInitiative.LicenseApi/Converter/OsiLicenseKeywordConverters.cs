using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Converter;

/// <summary>
/// Maps between <see cref="OsiLicenseKeyword"/> enum values and the OSI API string tokens.
/// </summary>
internal static class OsiLicenseKeywordMapping
{
    private static readonly Dictionary<OsiLicenseKeyword, string> ToToken = new()
    {
        [OsiLicenseKeyword.PopularStrongCommunity] = "popular-strong-community",
        [OsiLicenseKeyword.International] = "international",
        [OsiLicenseKeyword.SpecialPurpose] = "special-purpose",
        [OsiLicenseKeyword.NonReusable] = "non-reusable",
        [OsiLicenseKeyword.Superseded] = "superseded",
        [OsiLicenseKeyword.VoluntarilyRetired] = "voluntarily-retired",
        [OsiLicenseKeyword.RedundantWithMorePopular] = "redundant-with-more-popular",
        [OsiLicenseKeyword.OtherMiscellaneous] = "other-miscellaneous",
        [OsiLicenseKeyword.Uncategorized] = "uncategorized",
    };

    private static readonly Dictionary<string, OsiLicenseKeyword> FromToken = ToToken
        .ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    public static string ToApiValue(OsiLicenseKeyword keyword) => ToToken[keyword];

    public static bool TryParse(string? token, out OsiLicenseKeyword value)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            value = default;
            return false;
        }
        return FromToken.TryGetValue(token, out value);
    }
}

/// <summary>
/// JSON converter for a single <see cref="OsiLicenseKeyword"/> value.
/// </summary>
public sealed class OsiLicenseKeywordConverter : JsonConverter<OsiLicenseKeyword>
{
    public override OsiLicenseKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (OsiLicenseKeywordMapping.TryParse(s, out var value))
            return value;
        throw new JsonException($"Unknown OSI license keyword '{s}'");
    }

    public override void Write(Utf8JsonWriter writer, OsiLicenseKeyword value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(OsiLicenseKeywordMapping.ToApiValue(value));
    }
}

/// <summary>
/// JSON converter for a list of <see cref="OsiLicenseKeyword"/> values.
/// </summary>
public sealed class OsiLicenseKeywordsConverter : JsonConverter<List<OsiLicenseKeyword>>
{
    public override List<OsiLicenseKeyword> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new List<OsiLicenseKeyword>();

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for keywords");

        var list = new List<OsiLicenseKeyword>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            if (reader.TokenType == JsonTokenType.String)
            {
                var token = reader.GetString();
                if (OsiLicenseKeywordMapping.TryParse(token, out var value))
                {
                    list.Add(value);
                }
                else
                {
                    // Unknown tokens are ignored for forward-compatibility
                }
            }
            else
            {
                throw new JsonException("Expected string tokens in keywords array");
            }
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<OsiLicenseKeyword> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var v in value)
        {
            writer.WriteStringValue(OsiLicenseKeywordMapping.ToApiValue(v));
        }
        writer.WriteEndArray();
    }
}
