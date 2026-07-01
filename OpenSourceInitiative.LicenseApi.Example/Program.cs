using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;

Console.WriteLine("--- OpenSourceInitiative.LicenseApi Example ---\n");

// 1) Dependency Injection usage
Console.WriteLine("1) Dependency Injection usage\n");
var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

// Register with caching enabled by default
services.AddOsiLicensesClient();

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOsiClient>();

var allViaDi = await client.GetAllLicensesAsyncEnumerable().ToListAsync();
Console.WriteLine($"Loaded {allViaDi.Count} licenses (DI with caching).\n");

var mitViaDi = (await client.GetBySpdxIdAsync("MIT")).FirstOrDefault();
Console.WriteLine($"Lookup SPDX 'MIT': {(mitViaDi is null ? "not found" : mitViaDi.Name)}\n");

// Enum-based keyword filter via DI client
var popular = (await client.GetByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity)).ToList();
Console.WriteLine($"Popular licenses via DI: {popular.Count}\n");

// 2) Demonstrate different caching methods
Console.WriteLine("2) Demonstrate different caching methods\n");

var servicesNoCache = new ServiceCollection();
servicesNoCache.AddOsiLicensesClient();
await using var providerNoCache = servicesNoCache.BuildServiceProvider();
var clientNoCache = providerNoCache.GetRequiredService<IOsiClient>();
Console.WriteLine($"Client: {clientNoCache.GetType().Name}\n");

Console.WriteLine("Example completed.\n");