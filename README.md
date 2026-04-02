# OpenSourceInitiative.LicenseApi (C#/.NET)

[![CI](https://github.com/frequency403/OpenSourceInitiative.LicenseApi/actions/workflows/ci.yml/badge.svg)](https://github.com/frequency403/OpenSourceInitiative.LicenseApi/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A lightweight and resilient .NET client for the Open Source Initiative (OSI) License API. This library provides a
structured way to query OSI-approved licenses, including streaming support and automatic license text extraction.

## Features

* **Streaming API**: Support for `IAsyncEnumerable` to stream licenses, reducing memory usage when processing the full
  catalog.
* **License Text Extraction**: Automatically fetches and extracts clean, human-readable plain text from official OSI
  license HTML pages.
* **Extensible Caching**: Flexible caching layer supporting `IMemoryCache`, `IDistributedCache`, or a thread-safe
  in-memory fallback.
* **Comprehensive Filtering**: Query licenses by name, keyword, steward, or SPDX identifier (supporting wildcard
  patterns).
* **Strongly Typed**: Full mapping of OSI license metadata and classification keywords.
* **Resilient**: Designed for modern .NET with async-first patterns and minimal allocations.

## Installation

Install via NuGet:

```bash
dotnet add package OpenSourceInitiative.LicenseApi
```

Targets: `net10.0`, `netstandard2.0`

## Quickstart

### 1. Direct Usage (No Dependency Injection)

```csharp
using OpenSourceInitiative.LicenseApi.Clients;

// Create a client instance
using var client = new OsiClient();

// Stream all licenses
await foreach (var license in client.GetAllLicensesAsyncEnumerable())
{
    Console.WriteLine($"{license.SpdxId}: {license.Name}");
}

// Search by SPDX ID (supports wildcards)
var gplLicenses = await client.GetBySpdxIdAsync("GPL*");
```

### 2. Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;

var services = new ServiceCollection();

// Register the OSI client with caching enabled
services.AddOsiLicensesClient(options =>
{
    options.EnableCaching = true;
});

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOsiClient>();

// Get a license by its OSI identifier
var mit = await client.GetByOsiIdAsync("mit");
```

## API Reference

### `IOsiClient`

The primary interface for interacting with the OSI API.

* `IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable()`: Streams all registered licenses.
* `Task<OsiLicense?> GetByOsiIdAsync(string id)`: Retrieves a single license by its OSI ID (e.g., `mit`).
* `Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id)`: Filters by SPDX ID. Supports `*` wildcards.
* `Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name)`: Filters by name or partial name match.
* `Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword)`: Filters by OSI classification keyword.
* `Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward)`: Filters by the organization responsible for the
  license.

### Caching

Caching is enabled by default when using Dependency Injection. The library automatically detects and uses:

1. `IDistributedCache` (if registered)
2. `IMemoryCache` (if registered)
3. Internal thread-safe `ConcurrentDictionary` (fallback)

## Models

### `OsiLicense`

Key properties include:

* `Id`: Unique OSI identifier.
* `Name`: Full human-readable name.
* `SpdxId`: Standard SPDX identifier.
* `LicenseText`: Plain text content extracted from the license HTML page.
* `Keywords`: Collection of `OsiLicenseKeyword` classification tokens.
* `Approved`: Boolean indicating OSI approval status.
* `Links`: Metadata links to the API and official HTML representation.

## Build and Test

* **Build**: `dotnet build -c Release`
* **Test**: `dotnet test -c Release` (Requires coverage ≥ 75%)

## Contributing

Issues and pull requests are welcome. Please ensure:

* Code style remains consistent.
* New features are covered by tests.
* Branching follows [GitFlow](https://nvie.com/posts/a-successful-git-branching-model/).
* Contributors are added to the list below.

See [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for more details.

## License

MIT — see [LICENSE](LICENSE).

## Acknowledgements

Data is provided by the [Open Source Initiative (OSI) License API](https://opensource.org/api/).

## Contributors

* Oliver Schantz ([frequency403](https://github.com/frequency403))