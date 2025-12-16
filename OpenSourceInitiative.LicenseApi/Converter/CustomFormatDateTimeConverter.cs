using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSourceInitiative.LicenseApi.Converter;

/// <summary>
/// JSON converter for optional <see cref="DateTime"/> values encoded in the OSI format "yyyyMMdd".
/// </summary>
/// <remarks>
/// The OSI API encodes dates such as submission date and approval date using the compact pattern yyyyMMdd.
/// This converter reads that representation as nullable <see cref="DateTime"/> and writes the same format.
/// </remarks>
public class CustomFormatDateTimeConverter : JsonConverter<DateTime?>
{
    private const string DateFormat = "yyyyMMdd";

    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
            return null;

        if (DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        throw new JsonException($"Unable to parse '{dateString}' as date in format {DateFormat}");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
        else
            writer.WriteNullValue();
    }
}