using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Tests.Infrastructure;

namespace OpenSourceInitiative.LicenseApi.Tests.Integration;

public class LiveApiTests
{
    [OsiApiAvailableFact]
    public async Task GetAllLicenses_Matches_Expectations()
    {
        await using var client = new OsiLicensesClient(new OsiClient());
        var all = await client.GetAllLicensesAsync(CancellationToken.None);
        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();

        // Spot check known SPDX identifiers likely to exist
        var mit = await client.GetBySpdxAsync("MIT");
        mit.ShouldNotBeNull();
        string.IsNullOrWhiteSpace(mit.Name).ShouldBeFalse();

        // Search should return reasonable results
        var apache = await client.SearchAsync("Apache");
        apache.Count.ShouldBeGreaterThan(0);
    }

    [OsiApiAvailableFact]
    public async Task HtmlExtraction_Returns_Text()
    {
        await using var client = new OsiLicensesClient(new OsiClient());
        var mit = await client.GetBySpdxAsync("MIT");
        mit.ShouldNotBeNull();
        mit.LicenseText.ShouldNotBeNull();
        mit.LicenseText.Length.ShouldBeGreaterThan(0);
    }
}