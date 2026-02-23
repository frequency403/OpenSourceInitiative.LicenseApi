using System.Text.Json;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class OsiLicenseKeywordsConverterTests
{
    [Fact]
    public void Deserializes_Known_Tokens_And_Ignores_Unknown()
    {
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"keywords\":[\"popular-strong-community\",\"international\",\"unknown-token\"]",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        var lic = JsonSerializer.Deserialize<OsiLicense>(json);
        lic.ShouldNotBeNull();
        lic.Keywords.ShouldContain(OsiLicenseKeyword.PopularStrongCommunity);
        lic.Keywords.ShouldContain(OsiLicenseKeyword.International);
        lic.Keywords.ShouldNotContain(k => k.ToString().Equals("unknown-token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Serializes_Keywords_As_Tokens()
    {
        var lic = new OsiLicense
        {
            Id = "x",
            Name = "n",
            Keywords = [OsiLicenseKeyword.SpecialPurpose, OsiLicenseKeyword.Uncategorized],
            Links = new OsiLicenseLinks
            {
                Self = new OsiHref { Href = "s" },
                Html = new OsiHref { Href = "h" },
                Collection = new OsiHref { Href = "c" }
            }
        };

        var json = JsonSerializer.Serialize(lic);
        json.ShouldContain("\"keywords\":[\"special-purpose\",\"uncategorized\"]");
    }

    [Fact]
    public void Invalid_Keywords_Array_Element_Throws()
    {
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"keywords\":[1,2,3]",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        var action = () => JsonSerializer.Deserialize<OsiLicense>(json);
        action.ShouldThrow<JsonException>();
    }
}