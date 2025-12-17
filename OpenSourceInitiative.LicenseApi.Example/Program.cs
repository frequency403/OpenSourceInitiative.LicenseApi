using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;

Console.WriteLine("--- OpenSourceInitiative.LicenseApi Example ---\n");

// 1) Direct usage (no DI)
Console.WriteLine("1) Direct usage (no DI)\n");
using (var http = new HttpClient())
{
    // Base address is optional; client defaults to https://opensource.org/api/
    http.BaseAddress = new Uri("https://opensource.org/api/");
    await using (var direct = new OsiLicensesClient(http))
    {
        var all = await direct.GetAllLicensesAsync();
        Console.WriteLine($"Loaded {all.Count} licenses (direct).\n");

        var mit = await direct.GetBySpdxAsync("MIT");
        Console.WriteLine($"Lookup SPDX 'MIT': {(mit is null ? "not found" : mit.Name)}\n");

        var search = await direct.SearchAsync("Apache");
        Console.WriteLine($"Search 'Apache' returned {search.Count} result(s).\n");

        // New: server-side keyword filter using strongly-typed enum
        var popular = await direct.GetLicensesByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity);
        Console.WriteLine(
            $"Popular/strong-community licenses: {popular.Count} (first: {popular.FirstOrDefault()?.Name ?? "n/a"})\n");
    }
}

// 2) Using DI extensions to register typed client
Console.WriteLine("2) Dependency Injection usage\n");
var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());
services.AddOsiLicensesClient(
//     o =>
// {
//     // Optional configuration
//     //o.BaseAddress = new Uri("https://opensource.org/api/");
//     //o.PrimaryHandlerFactory = () => new HttpClientHandler { AllowAutoRedirect = false };
// }
);

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOsiLicensesClient>();

var allViaDi = await client.GetAllLicensesAsync();
Console.WriteLine($"Loaded {allViaDi.Count} licenses (DI).\n");

var mitViaDi = client.GetBySpdx("MIT"); // sync variant
Console.WriteLine($"Sync lookup SPDX 'MIT': {(mitViaDi is null ? "not found" : mitViaDi.Name)}\n");

// Enum-based keyword filter via DI client
var eclipsePopular = await client.GetLicensesByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity);
Console.WriteLine($"Popular licenses via DI: {eclipsePopular.Count}\n");

Console.WriteLine("Example completed.\n");