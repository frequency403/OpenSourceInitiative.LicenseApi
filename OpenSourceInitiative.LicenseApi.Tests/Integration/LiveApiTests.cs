using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Tests.Infrastructure;

namespace OpenSourceInitiative.LicenseApi.Tests.Integration;

public class LiveApiTests
{
    [OsiApiAvailableFact]
    public async Task GetAllLicenses_Matches_Expectations()
    {
        await using var client = new OsiClient();
        var all = new List<OsiLicense?>();
        await foreach (var license in client.GetAllLicensesAsyncEnumerable())
        {
            all.Add(license);
        }
        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();

        // Spot check known SPDX identifiers likely to exist
        var mitResults = await client.GetBySpdxIdAsync("MIT");
        var mit = mitResults.FirstOrDefault(x => x?.SpdxId == "MIT");
        mit.ShouldNotBeNull();
        string.IsNullOrWhiteSpace(mit.Name).ShouldBeFalse();
    }

    [OsiApiAvailableFact]
    public async Task HtmlExtraction_Returns_Text()
    {
        await using var client = new OsiClient();
        var mitResults = await client.GetBySpdxIdAsync("MIT");
        var mit = mitResults.FirstOrDefault(x => x?.SpdxId == "MIT");
        mit.ShouldNotBeNull();
        mit.LicenseText.ShouldNotBeNull();
        mit.LicenseText.Length.ShouldBeGreaterThan(0);
    }
}