using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Tests.Infrastructure;

namespace OpenSourceInitiative.LicenseApi.Tests.Integration;

public class LiveApiTests
{
    [OsiApiAvailableFact]
    public async Task GetAllLicenses_Matches_Expectations()
    {
        await using var osiClient = new OsiClient();
        await using var client = new OsiLicensesClient(osiClient);
        var all = await client.GetAllLicensesAsync(CancellationToken.None);
        Assert.NotNull(all);
        Assert.NotEmpty(all);

        // Spot check known SPDX identifiers likely to exist
        var mit = await client.GetBySpdxAsync("MIT");
        Assert.NotNull(mit);
        Assert.False(string.IsNullOrWhiteSpace(mit.Name));

        // Search should return reasonable results
        var apache = await client.SearchAsync("Apache");
        Assert.True(apache.Count > 0);
    }

    [OsiApiAvailableFact]
    public async Task HtmlExtraction_Returns_Text()
    {
        using var http = new HttpClient();
        await using var osiClient = new OsiClient(httpClient: http);
        var mit = await osiClient.GetByOsiIdAsync("mit");
        Assert.NotNull(mit);
        var text = mit!.LicenseText;
        Assert.NotNull(text);
        Assert.True(text.Length > 0);
    }
}