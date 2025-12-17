using System.Text.Json;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class CustomFormatDateTimeConverterTests
{
    [Fact]
    public void Deserializes_Dates_In_yyyyMMdd_Format()
    {
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"submission_date\":\"20250201\"",
            "\"approval_date\":\"20250302\"",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        var lic = JsonSerializer.Deserialize<OsiLicense>(json);
        Assert.NotNull(lic);
        Assert.Equal(new DateTime(2025, 2, 1), lic.SubmissionDate);
        Assert.Equal(new DateTime(2025, 3, 2), lic.ApprovalDate);
    }

    [Fact]
    public void Null_Or_Empty_Dates_Parse_As_Null()
    {
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"submission_date\":null",
            "\"approval_date\":\"\"",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        var lic = JsonSerializer.Deserialize<OsiLicense>(json);
        Assert.NotNull(lic);
        Assert.Null(lic.SubmissionDate);
        Assert.Null(lic.ApprovalDate);
    }

    [Fact]
    public void Invalid_Date_Format_Throws_JsonException()
    {
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"submission_date\":\"2025-02-01\"",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<OpenSourceInitiative.LicenseApi.Models.OsiLicense>(json));
    }

    [Fact]
    public void Serializes_Dates_In_yyyyMMdd_Format()
    {
        var lic = new OpenSourceInitiative.LicenseApi.Models.OsiLicense
        {
            Id = "x",
            Name = "n",
            SubmissionDate = new DateTime(2024, 12, 31),
            ApprovalDate = new DateTime(2025, 1, 1),
            Links = new()
            {
                Self = new() { Href = "s" },
                Html = new() { Href = "h" },
                Collection = new() { Href = "c" }
            }
        };

        var json = JsonSerializer.Serialize(lic);
        Assert.Contains("\"submission_date\":\"20241231\"", json);
        Assert.Contains("\"approval_date\":\"20250101\"", json);
    }
}