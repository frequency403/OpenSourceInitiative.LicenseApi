using System.Text.Json;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class CustomFormatDateTimeConverterTests
{
    [Fact]
    public void Deserializes_Dates_In_yyyyMMdd_Format()
    {
        // Arrange
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"submission_date\":\"20250201\"",
            "\"approval_date\":\"20250302\"",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        // Act
        var lic = JsonSerializer.Deserialize<OsiLicense>(json);

        // Assert
        lic.Should().NotBeNull();
        lic!.SubmissionDate.Should().Be(new DateTime(2025, 2, 1));
        lic.ApprovalDate.Should().Be(new DateTime(2025, 3, 2));
    }

    [Fact]
    public void Null_Or_Empty_Dates_Parse_As_Null()
    {
        // Arrange
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"submission_date\":null",
            "\"approval_date\":\"\"",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        // Act
        var lic = JsonSerializer.Deserialize<OsiLicense>(json);

        // Assert
        lic.Should().NotBeNull();
        lic!.SubmissionDate.Should().BeNull();
        lic.ApprovalDate.Should().BeNull();
    }

    [Fact]
    public void Invalid_Date_Format_Throws_JsonException()
    {
        // Arrange
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"submission_date\":\"2025-02-01\"",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        // Act
        var act = () => JsonSerializer.Deserialize<OsiLicense>(json);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Serializes_Dates_In_yyyyMMdd_Format()
    {
        // Arrange
        var lic = new OsiLicense
        {
            Id = "x",
            Name = "n",
            SubmissionDate = new DateTime(2024, 12, 31),
            ApprovalDate = new DateTime(2025, 1, 1),
            Links = new OsiLicenseLinks
            {
                Self = new OsiHref { Href = "s" },
                Html = new OsiHref { Href = "h" },
                Collection = new OsiHref { Href = "c" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(lic);

        // Assert
        json.Should().Contain("\"submission_date\":\"20241231\"");
        json.Should().Contain("\"approval_date\":\"20250101\"");
    }
}