using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Tests.Infrastructure;

namespace OpenSourceInitiative.LicenseApi.Tests.Integration;

public class LiveApiTests(ITestOutputHelper outputHelper)
{
    private readonly ILogger<OsiClient> _fakeLogger = new FakeLogger<OsiClient>(outputHelper.WriteLine);
    [OsiApiAvailableFact]
    public async Task GetAllLicenses_Matches_Expectations()
    {
        await using var client = new OsiClient(logger: _fakeLogger);
        var all = new List<OsiLicense?>();
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var sw = Stopwatch.StartNew();
        await foreach (var license in client.GetAllLicensesAsyncEnumerable(linkedToken.Token))
        {
            var innerSw = Stopwatch.StartNew();
            all.Add(license);
            innerSw.Stop();
            _fakeLogger.LogDebug("Fetched license {SpdxId} in {Elapsed}ms", license?.SpdxId, innerSw.Elapsed.TotalMilliseconds);
        }
        sw.Stop();
        _fakeLogger.LogInformation("Fetched {Count} licenses in {Elapsed}", all.Count, sw.Elapsed);
        
        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();

        // Spot check known SPDX identifiers likely to exist
        var mitResults = await client.GetBySpdxIdAsync("MIT", linkedToken.Token);
        var mit = mitResults.FirstOrDefault(x => x?.SpdxId == "MIT");
        mit.ShouldNotBeNull();
        string.IsNullOrWhiteSpace(mit.Name).ShouldBeFalse();
        
        _fakeLogger.LogInformation("{Count} licenses with license text", all.Count(x => !string.IsNullOrWhiteSpace(x?.LicenseText)));
        foreach (var license in all.Where(x => !string.IsNullOrWhiteSpace(x?.LicenseText)))
        {
            _fakeLogger.LogInformation("License {SpdxId} has text\n{Text}", license?.SpdxId, license?.LicenseText);
        }
    }

    [OsiApiAvailableFact]
    public async Task HtmlExtraction_Returns_Text()
    {
        await using var client = new OsiClient(_fakeLogger);
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var mitResults = await client.GetBySpdxIdAsync("MIT", linkedToken.Token);
        var mit = mitResults.FirstOrDefault(x => x?.SpdxId == "MIT");
        mit.ShouldNotBeNull();
        mit.LicenseText.ShouldNotBeNull();
        mit.LicenseText.Length.ShouldBeGreaterThan(0);
    }
}