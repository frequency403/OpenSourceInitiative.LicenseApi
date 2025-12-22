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
    await using (var direct = new OsiClient(httpClient: http))
    {
        var all = await direct.GetAllLicensesAsyncEnumerable().ToListAsync();
        Console.WriteLine($"Loaded {all.Count} licenses (direct).\n");

        var mit = (await direct.GetBySpdxIdAsync("MIT")).FirstOrDefault();
        Console.WriteLine($"Lookup SPDX 'MIT': {(mit is null ? "not found" : mit.Name)}\n");

        var search = (await direct.GetByNameAsync("Apache")).ToList();
        Console.WriteLine($"Search 'Apache' returned {search.Count} result(s).\n");

        // New: server-side keyword filter using strongly-typed enum
        var popular = (await direct.GetByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity)).ToList();
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
var client = provider.GetRequiredService<IOsiClient>();
var allViaDi = await client.GetAllLicensesAsyncEnumerable().ToListAsync();
Console.WriteLine($"Loaded {allViaDi.Count} licenses (DI).\n");

var mitViaDi = (await client.GetBySpdxIdAsync("MIT")).FirstOrDefault();
Console.WriteLine($"Lookup SPDX 'MIT': {(mitViaDi is null ? "not found" : mitViaDi.Name)}\n");

// Enum-based keyword filter via DI client
var eclipsePopular = (await client.GetByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity)).ToList();
Console.WriteLine($"Popular licenses via DI: {eclipsePopular.Count}\n");

Console.WriteLine("Example completed.\n");