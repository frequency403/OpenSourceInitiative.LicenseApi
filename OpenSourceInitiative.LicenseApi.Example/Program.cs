using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;

Console.WriteLine("--- OpenSourceInitiative.LicenseApi Example ---\n");

// 1) Dependency Injection usage
Console.WriteLine("1) Dependency Injection usage\n");
var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

// Register with caching enabled by default
services.AddOsiLicensesClient(options =>
{
    options.EnableCaching = true;
});

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

// Example of registering without caching
var servicesNoCache = new ServiceCollection();
servicesNoCache.AddOsiLicensesClient(options => options.EnableCaching = false);
await using var providerNoCache = servicesNoCache.BuildServiceProvider();
var clientNoCache = providerNoCache.GetRequiredService<IOsiClient>();
Console.WriteLine($"Client without caching: {clientNoCache.GetType().Name}\n");

// Example of registering with memory cache
var servicesMemoryCache = new ServiceCollection();
servicesMemoryCache.AddMemoryCache(); // Required for MemoryCacheAdapter
servicesMemoryCache.AddOsiLicensesClient(options => options.EnableCaching = true);
await using var providerMemoryCache = servicesMemoryCache.BuildServiceProvider();
var clientMemoryCache = providerMemoryCache.GetRequiredService<IOsiClient>();
Console.WriteLine($"Client with memory caching: {clientMemoryCache.GetType().Name}\n");

Console.WriteLine("Example completed.\n");