 # OpenSourceInitiative.LicenseApi (C#/.NET)

 [![CI](https://github.com/frequency403/OpenSourceInitiative.LicenseApi/actions/workflows/ci.yml/badge.svg)](https://github.com/frequency403/OpenSourceInitiative.LicenseApi/actions/workflows/ci.yml)
 [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

 A lightweight, resilient .NET client for the Open Source Initiative (OSI) License API, plus optional Dependency Injection (DI) extensions.

 This repository contains:
 * OpenSourceInitiative.LicenseApi — the core typed client to query OSI licenses
 * OpenSourceInitiative.LicenseApi.DependencyInjection — DI extensions to register the client via IHttpClientFactory
 * OpenSourceInitiative.LicenseApi.Example — a small console app demonstrating direct and DI usage
 * OpenSourceInitiative.LicenseApi.Tests — unit and integration tests

 ## Why this library?
 * Fetches the OSI license catalog and extracts human‑readable license text from the HTML page of each license
 * Keeps an in‑memory, thread‑safe cache once data is loaded; subsequent queries operate on the snapshot
 * Fail‑safe networking: on errors, you get the last available snapshot (possibly empty) instead of exceptions bubbling up
 * Async API with synchronous counterparts for convenient use across environments
 * Server‑side filtering endpoints supported (name, keyword, steward, SPDX wildcard pattern)
 * Strongly‑typed keyword filtering via `OsiLicenseKeyword` enum

 ## Install
 Core client:

 ```bash
 dotnet add package OpenSourceInitiative.LicenseApi
 ```

 Dependency Injection extensions (optional):

 ```bash
 dotnet add package OpenSourceInitiative.LicenseApi.DependencyInjection
 ```

 Targets: `net10.0`, `netstandard2.0` (broad platform compatibility)

 ## Quickstart
 ### 1) Direct usage (no DI)
 ```csharp
 using OpenSourceInitiative.LicenseApi.Clients;

 using var http = new HttpClient { BaseAddress = new Uri("https://opensource.org/api/") }; // optional; defaults to this
 await using var client = new OsiLicensesClient(http);

 // Load all
 var licenses = await client.GetAllLicensesAsync();

 // Search by name or id (cached, case-insensitive)
 var apache = await client.SearchAsync("Apache");

 // Lookup by SPDX (sync/async)
 var mit = client.GetBySpdx("MIT");
 var mitAsync = await client.GetBySpdxAsync("MIT");

 // Server-side filters
 var byName = await client.GetLicensesByNameAsync("mit");
 var bySteward = await client.GetLicensesByStewardAsync("eclipse-foundation");
 var bySpdxPattern = await client.GetLicensesBySpdxPatternAsync("gpl*");

 // Strongly-typed keyword filter
 var popular = await client.GetLicensesByKeywordAsync(OpenSourceInitiative.LicenseApi.Enums.OsiLicenseKeyword.PopularStrongCommunity);
 ```

 ### 2) Using Dependency Injection
 ```csharp
 using Microsoft.Extensions.DependencyInjection;
 using Microsoft.Extensions.Logging;
 using OpenSourceInitiative.LicenseApi.DependencyInjection.Extensions;
 using OpenSourceInitiative.LicenseApi.Interfaces;

 var services = new ServiceCollection();
 services.AddLogging(b => b.AddConsole());

 services.AddOsiLicensesClient(o =>
 {
     // o.BaseAddress = new Uri("https://opensource.org/api/"); // optional (this is the default)
     o.EnableLogging = true; // optional lightweight request/response logging handler
 });

 await using var provider = services.BuildServiceProvider();
 var client = provider.GetRequiredService<IOsiLicensesClient>();

 var all = await client.GetAllLicensesAsync();
 var mit = await client.GetBySpdxAsync("MIT");
 var search = client.Search("Apache"); // sync variant
 ```

 See a runnable demo in `OpenSourceInitiative.LicenseApi.Example`.

 ## API at a glance
 The main abstraction is `IOsiLicensesClient` with both async and sync methods:
 * Initialization/cache
   * `Task InitializeAsync(CancellationToken)` / `void Initialize()`
   * `IReadOnlyList<OsiLicense> Licenses` — current immutable snapshot
 * Catalog and search
   * `Task<IReadOnlyList<OsiLicense>> GetAllLicensesAsync(...)`
   * `Task<IReadOnlyList<OsiLicense>> SearchAsync(string query, ...)` and `IReadOnlyList<OsiLicense> Search(string query)`
   * `Task<OsiLicense?> GetBySpdxAsync(string spdxId, ...)` and `OsiLicense? GetBySpdx(string spdxId)`
 * Server‑side filters (per OSI API)
   * `GetLicensesByNameAsync(string name, ...)`
   * `GetLicensesByKeywordAsync(string keyword, ...)` and `GetLicensesByKeywordAsync(OsiLicenseKeyword keyword, ...)`
   * `GetLicensesByStewardAsync(string steward, ...)`
   * `GetLicensesBySpdxPatternAsync(string spdxPattern, ...)` (supports `*` wildcards)

 ## Models
 `OsiLicense` (selected properties):
 * `string Id` — OSI unique identifier
 * `string Name` — human‑readable license name
 * `string? SpdxId` — SPDX id, e.g., `MIT`, `Apache-2.0`
 * `string? Version`
 * `DateTime? SubmissionDate`, `string? SubmissionUrl`, `string? SubmitterName`
 * `bool Approved`, `DateTime? ApprovalDate`
 * `List<string> Stewards`
 * `List<OsiLicenseKeyword> Keywords` — mapped from OSI keyword tokens
 * `OsiLicenseLinks Links` — links to self page, public HTML page, and collection
 * `string LicenseText` — extracted plaintext of the license HTML page

 Keyword enum (`OsiLicenseKeyword`) covers OSI classifications, including:
 `PopularStrongCommunity`, `International`, `SpecialPurpose`, `NonReusable`, `Superseded`, `VoluntarilyRetired`, `RedundantWithMorePopular`, `OtherMiscellaneous`, `Uncategorized`.

 ## Configuration (DI)
 When using `AddOsiLicensesClient(...)` you can configure:
 * `BaseAddress` — defaults to `https://opensource.org/api/`
 * `EnableLogging` — adds a lightweight delegating handler that logs request/response metadata
 * `PrimaryHandlerFactory` — supply a custom `HttpMessageHandler` (e.g., for tests)

 ## Example project
 * `OpenSourceInitiative.LicenseApi.Example/Program.cs` demonstrates direct and DI usage, search, SPDX lookup, and keyword filtering.

 ## Build, test, and CI
 * Build locally: `dotnet build -c Release`
 * Run tests: `dotnet test -c Release`
 * CI: GitHub Actions builds on Ubuntu, Windows, and macOS with .NET SDK 9 and 10, runs tests with line coverage threshold and uploads Cobertura coverage artifacts.

 Some integration tests hit the live OSI API (see `OpenSourceInitiative.LicenseApi.Tests/Integration`). They are decorated to run only when the API is reachable.

 ## Repository layout
 * `OpenSourceInitiative.LicenseApi/` — core library (see its [README](OpenSourceInitiative.LicenseApi/README.md))
 * `OpenSourceInitiative.LicenseApi.DependencyInjection/` — DI extensions (see its [README](OpenSourceInitiative.LicenseApi.DependencyInjection/README.md))
 * `OpenSourceInitiative.LicenseApi.Example/` — runnable example
 * `OpenSourceInitiative.LicenseApi.Tests/` — unit and integration tests

 ## Versioning
 The project follows semantic versioning. Public API changes will result in a major version bump.

 ## Contributing
 Issues and pull requests are welcome. If you contribute, please:
 * Add or update tests
 * Keep code style consistent with the surrounding code
 * Ensure `dotnet test` passes (CI enforces coverage ≥ 75% lines)

 See also: [CONTRIBUTING.md](CONTRIBUTING.md) and our [Code of Conduct](CODE_OF_CONDUCT.md).

 ## License
 MIT — see [LICENSE](LICENSE).

 ## Acknowledgements
 * Data is provided by the Open Source Initiative (OSI) License API.

 ## Community & Docs
 - Contributing guide: [CONTRIBUTING.md](CONTRIBUTING.md)
 - Code of Conduct: [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
 - Security policy: [SECURITY.md](SECURITY.md)
 - Support: [SUPPORT.md](SUPPORT.md)
 - Changelog: [CHANGELOG.md](CHANGELOG.md)