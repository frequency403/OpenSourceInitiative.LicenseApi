using System.Text.Json;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class OsiLicenseKeywordsSerializationTests
{
    [Fact]
    public void Deserializes_Keywords_As_Enum_List()
    {
        var json = "{" + string.Join(',',
            "\"id\":\"x\"",
            "\"name\":\"Name\"",
            "\"keywords\":[\"popular-strong-community\",\"international\"]",
            "\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}"
        ) + "}";

        var lic = JsonSerializer.Deserialize<OsiLicense>(json);
        Assert.NotNull(lic);
        Assert.NotNull(lic.Keywords);
        Assert.Contains(OsiLicenseKeyword.PopularStrongCommunity, lic.Keywords);
        Assert.Contains(OsiLicenseKeyword.International, lic.Keywords);
    }
}