# OpenSourceInitiative.LicenseApi

Lightweight, resilient client for the Open Source Initiative (OSI) License API.

Highlights:

- Fetches the OSI license catalog and extracts human‑readable license text from the HTML pages
- In‑memory, thread‑safe cache with fail‑safe behavior (returns last snapshot on network errors)
- Search by name or id and lookup by SPDX identifier
- Async API plus synchronous counterparts
- Targets .NET 10 and .NET Standard 2.0

Install:

```bash
dotnet add package OpenSourceInitiative.LicenseApi
```

Basic usage:

```csharp
using OpenSourceInitiative.LicenseApi.Clients;

var http = new HttpClient { BaseAddress = new Uri("https://opensource.org/api/") };
using var client = new OsiLicensesClient(http);

// Load all
var licenses = await client.GetAllLicensesAsync();

// Search by name or id
var apache = await client.SearchAsync("Apache");

// Lookup by SPDX (sync/async)
var mit = client.GetBySpdx("MIT");
var mitAsync = await client.GetBySpdxAsync("MIT");
```

Dependency Injection (recommended):

Register the client as a typed HTTP client using the built‑in `ServiceCollection` extensions in this package.

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;

var services = new ServiceCollection();
services.AddLogging();
services.AddOsiLicensesClient(o =>
{
    // o.BaseAddress = new Uri("https://opensource.org/api/");
});

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOsiLicensesClient>();

var all = await client.GetAllLicensesAsync();
var byName = client.Search("Apache"); // sync search
var mit = await client.GetBySpdxAsync("MIT");
```

Example project

`OpenSourceInitiative.LicenseApi.Example` demonstrates both direct and DI usage, including search and SPDX lookup.
