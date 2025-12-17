# OpenSourceInitiative.LicenseApi.DependencyInjection

DI extensions for `OpenSourceInitiative.LicenseApi`.

Adds `AddOsiLicensesClient(...)` to `IServiceCollection` to register a typed `IOsiLicensesClient` using
`IHttpClientFactory`, with optional logging, base address configuration, and custom primary handler.

Install:

```bash
dotnet add package OpenSourceInitiative.LicenseApi.DependencyInjection
```

Options:

- `BaseAddress` (default: `https://opensource.org/api/`)
- `PrimaryHandlerFactory` (inject a custom `HttpMessageHandler` – useful for tests)

Model notes:

- `OsiLicense.Keywords` is a strongly-typed `List<OsiLicenseKeyword>` mapped from the OSI API keyword tokens.
  Available values:
    - `PopularStrongCommunity` → `"popular-strong-community"`
    - `International` → `"international"`
    - `SpecialPurpose` → `"special-purpose"`
    - `NonReusable` → `"non-reusable"`
    - `Superseded` → `"superseded"`
    - `VoluntarilyRetired` → `"voluntarily-retired"`
    - `RedundantWithMorePopular` → `"redundant-with-more-popular"`
    - `OtherMiscellaneous` → `"other-miscellaneous"`
    - `Uncategorized` → `"uncategorized"`

Example:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.DependencyInjection.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;

var services = new ServiceCollection();

services.AddLogging();
services.AddOsiLicensesClient(o =>
{
    // o.BaseAddress = new Uri("https://opensource.org/api/"); // optional
});

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOsiLicensesClient>();

// Async + sync APIs
var all = await client.GetAllLicensesAsync();
var mit = client.GetBySpdx("MIT");
var apache = await client.SearchAsync("Apache");

// Server-side filtered queries per OSI spec
var mitNamed = await client.GetLicensesByNameAsync("mit");
var popular = await client.GetLicensesByKeywordAsync("popular-strong-community");
var eclipse = await client.GetLicensesByStewardAsync("eclipse-foundation");
var gpls = await client.GetLicensesBySpdxPatternAsync("gpl*");

// Overload: use enum for keyword filter
var popularEnum = await client.GetLicensesByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity);
```
