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
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
#if !DEBUG
        linkedToken.CancelAfter(TimeSpan.FromSeconds(30));
#endif
        await foreach (var license in client.GetAllLicensesAsyncEnumerable(linkedToken.Token))
        {
            all.Add(license);
        }

        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();

        // Spot check known SPDX identifiers likely to exist
        var mitResults = await client.GetBySpdxIdAsync("MIT", linkedToken.Token);
        var mit = mitResults.FirstOrDefault(x => x?.SpdxId == "MIT");
        mit.ShouldNotBeNull();
        string.IsNullOrWhiteSpace(mit.Name).ShouldBeFalse();
    }

    [OsiApiAvailableFact]
    public async Task HtmlExtraction_Returns_Text()
    {
        await using var client = new OsiClient();
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var mitResults = await client.GetBySpdxIdAsync("MIT", linkedToken.Token);
        var mit = mitResults.FirstOrDefault(x => x?.SpdxId == "MIT");
        mit.ShouldNotBeNull();
        mit.LicenseText.ShouldNotBeNull();
        mit.LicenseText.Length.ShouldBeGreaterThan(0);
    }
}