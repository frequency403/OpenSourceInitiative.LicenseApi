using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSourceInitiative.LicenseApi.Converter;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class CustomFormatDateTimeConverterTests
{
    private static JsonSerializerOptions OptionsWithConverter()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        opts.Converters.Add(new CustomFormatDateTimeConverter());
        return opts;
    }

    [Fact]
    public void Deserializes_Dates_In_yyyyMMdd_Format()
    {
        // Arrange
        var json = "{" + string.Join(',',
            "\"submission_date\":\"20250201\"",
            "\"approval_date\":\"20250302\""
        ) + "}";

        // Act
        var obj = JsonSerializer.Deserialize<Temp>(json, OptionsWithConverter());

        // Assert
        obj.ShouldNotBeNull();
        obj!.SubmissionDate.ShouldBe(new DateTime(2025, 2, 1));
        obj.ApprovalDate.ShouldBe(new DateTime(2025, 3, 2));
    }

    [Fact]
    public void Null_Or_Empty_Dates_Parse_As_Null()
    {
        // Arrange
        var json = "{" + string.Join(',',
            "\"submission_date\":null",
            "\"approval_date\":\"\""
        ) + "}";

        // Act
        var obj = JsonSerializer.Deserialize<Temp>(json, OptionsWithConverter());

        // Assert
        obj.ShouldNotBeNull();
        obj!.SubmissionDate.ShouldBeNull();
        obj.ApprovalDate.ShouldBeNull();
    }

    [Fact]
    public void Invalid_Date_Format_Throws_JsonException()
    {
        // Arrange
        var json = "{" + string.Join(',',
            "\"submission_date\":\"2025-02-01\""
        ) + "}";

        // Act
        var act = () => JsonSerializer.Deserialize<Temp>(json, OptionsWithConverter());

        // Assert
        act.ShouldThrow<JsonException>();
    }

    [Fact]
    public void Serializes_Dates_In_yyyyMMdd_Format()
    {
        // Arrange
        var obj = new Temp
        {
            SubmissionDate = new DateTime(2024, 12, 31),
            ApprovalDate = new DateTime(2025, 1, 1)
        };

        // Act
        var json = JsonSerializer.Serialize(obj, OptionsWithConverter());

        // Assert
        json.ShouldContain("\"submission_date\":\"20241231\"");
        json.ShouldContain("\"approval_date\":\"20250101\"");
    }

    private record Temp
    {
        [JsonPropertyName("submission_date")] public DateTime? SubmissionDate { get; init; }
        [JsonPropertyName("approval_date")] public DateTime? ApprovalDate { get; init; }
    }
}