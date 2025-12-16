using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Infrastructure;

namespace OpenSourceInitiative.LicenseApi.Tests.Integration;

public class LiveApiTests
{
    private const string ApiBase = "https://opensource.org/api/license";

    [OsiApiAvailableFact]
    public async Task GetAllLicenses_Matches_Expectations()
    {
        await using var client = new OsiLicensesClient();
        var all = await client.GetAllLicensesAsync(CancellationToken.None);
        Assert.NotNull(all);
        Assert.NotEmpty(all);

        // Spot check known SPDX identifiers likely to exist
        var mit = await client.GetBySpdxAsync("MIT");
        Assert.NotNull(mit);
        Assert.False(string.IsNullOrWhiteSpace(mit!.Name));

        // Search should return reasonable results
        var apache = await client.SearchAsync("Apache");
        Assert.True(apache.Count > 0);
    }

    [OsiApiAvailableFact]
    public async Task HtmlExtraction_Returns_Text()
    {
        await using var client = new OsiLicensesClient();
        var mit = await client.GetBySpdxAsync("MIT");
        Assert.NotNull(mit);
        Assert.NotNull(mit!.LicenseText);
        Assert.True(mit.LicenseText.Length > 0);
    }
}
