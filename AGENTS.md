# AGENTS.md

Guidance for AI coding agents (and humans) working in this repository.

## What this is

`OpenSourceInitiative.LicenseApi` is a small, multi-targeted .NET client library for the
[Open Source Initiative License API](https://opensource.org/api), published to NuGet.
It fetches the OSI license catalog, filters by SPDX ID / name / keyword / steward, and scrapes
human-readable license text from the OSI HTML pages (HtmlAgilityPack). There is **no caching
layer** — every call hits the network; callers add their own decorator if they need caching.

`README.md` is the authoritative description of the public API surface and is packed into the
NuGet package. Keep it in sync with any public API change.

## Repository layout

| Path | Purpose |
|------|---------|
| `OpenSourceInitiative.LicenseApi/` | The library (packable). Targets `net10.0;netstandard2.0`. |
| `  Clients/` | `OsiClient` (primary, `IOsiClient`) and the `[Obsolete]` `OsiLicensesClient` (`IOsiLicensesClient`). |
| `  Converter/` | `System.Text.Json` converters (`yyyyMMdd` dates, keyword tokens). |
| `  Enums/`, `Models/`, `Options/`, `Interfaces/`, `Exceptions/` | Public types. |
| `  Extensions/` | `ServiceCollectionExtensions.AddOsiLicensesClient`, `HttpClientExtensions` (headers, license-text scraping), `OsiLicenseExtensions`. |
| `  Log/LoggerMethods.cs` | Source-generated `[LoggerMessage]` methods. |
| `  IsExternalInit.cs` | Polyfill so `init` works on `netstandard2.0`. |
| `OpenSourceInitiative.LicenseApi.Tests/` | xUnit v3 tests (`net10.0`), Shouldly + NSubstitute. |
| `  Utils/StubHttpMessageHandler.cs` | In-memory HTTP handler — use this instead of real network calls. |
| `  Integration/LiveApiTests.cs` | Live tests against opensource.org, gated by `[OsiApiAvailableFact]`. |
| `OpenSourceInitiative.LicenseApi.Example/` | Runnable console sample (not packable). |
| `openapi.yml`, `swagger.json` | OSI API specification for reference. |
| `Directory.Build.props` | **Package version** and NuGet metadata for all projects. |
| `Directory.Packages.props` | Central Package Management — **all** NuGet versions live here. |
| `global.json` | SDK 10.0.100, `rollForward: latestMajor`. |
| `OpenSourceInitiative.LicenseApi.slnx` | Solution (new XML format). |

## Build, test, run

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet run --project OpenSourceInitiative.LicenseApi.Example
```

CI (`.github/workflows/ci.yml`) runs on Ubuntu, Windows and macOS and enforces line coverage
via Coverlet:

```bash
dotnet test -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=75 /p:ThresholdType=line /p:ThresholdStat=average
```

- `GeneratePackageOnBuild` is on, so every build drops `.nupkg`/`.snupkg` into `nupkg/` (gitignored).
- Live API tests are skipped automatically when opensource.org is unreachable. Set
  `OSI_API_TESTS=1` to force them to run.

## Coding conventions

- **Multi-targeting:** code must compile for both `net10.0` and `netstandard2.0`. Guard
  API differences with `#if !NETSTANDARD2_0` (see `ArgumentNullException.ThrowIfNull`,
  `ReadAsStreamAsync(token)`, `await using`). `System.Text.Json` is a package dependency only
  for `netstandard2.0`.
- `LangVersion` is `latest`, `Nullable` and `ImplicitUsings` are enabled. Use nullable
  annotations (`?`), not JetBrains nullability attributes. JetBrains.Annotations is referenced
  only for things like `[UsedImplicitly]`.
- File-scoped namespaces matching the folder (`OpenSourceInitiative.LicenseApi.<Folder>`).
- Public types and members get XML doc comments (`<inheritdoc />` on interface implementations).
- Logging: add new messages to `Log/LoggerMethods.cs` as `[LoggerMessage]` partial methods
  rather than ad-hoc string logging where practical.
- Scraping failures for a single license must not fail the whole call — `LicenseText` falls
  back to an empty string.
- Caller-supplied `id`/`name`/`steward`/`keyword` values are escaped before being combined
  with `BaseAddress`; keep that guarantee when touching request building.
- DI: `AddOsiLicensesClient` registers a typed `IHttpClientFactory` client plus
  `IOsiClient → OsiClient` (transient). `OsiClientOptions.PrimaryHandlerFactory` exists for
  testability.

## Testing conventions

- xUnit v3 (`[Fact]`, `[Theory]`, `TestContext.Current.CancellationToken`), Shouldly
  assertions (`x.ShouldBe(...)`), NSubstitute for mocks. `Shouldly` and `Xunit` are global usings.
- Arrange / Act / Assert sections with comments; test names like
  `Method_Condition_ExpectedResult`.
- Unit tests must not hit the network — use `StubHttpMessageHandler`. Anything that needs the
  real API goes in `Integration/` with `[OsiApiAvailableFact]`.
- Add or update tests with every behavioural change; keep coverage above the CI threshold.
- The test project sets `IsTestingPlatformApplication=false`: xunit.v3 4.x defaults to
  Microsoft.Testing.Platform, which refuses the VSTest-based `dotnet test` on the .NET 10 SDK.
  Keeping VSTest lets `coverlet.msbuild` and the `/p:CollectCoverage=...` CI command line work.
  Don't remove it without migrating CI coverage to an MTP extension.
- In Git Bash, `/p:` switches get mangled by path conversion — prefix commands with
  `MSYS_NO_PATHCONV=1` or use PowerShell.

## Dependencies

- Never put a `Version` on a `<PackageReference>`; add/update the `<PackageVersion>` in
  `Directory.Packages.props`. Transitive pinning is enabled.
- Keep the `Microsoft.Extensions.*` / `System.Text.Json` / `Microsoft.Bcl.AsyncInterfaces`
  family on the same patch version.
- Dependabot only reads its config from the GitHub **default branch** (`main`). Its PRs
  therefore target `main`; resolve them by applying the bumps on `develop` and closing the PR.

## Branching, versioning and releases

- `develop` is the integration branch — all work (including dependency bumps) lands here.
- `main` is the release branch. A PR `develop → main` (often via `release/vX.Y.Z`) triggers
  `version-guard.yml`, which fails unless `<Version>` in `Directory.Build.props` is **greater**
  than on `main`.
- Merging into `main` creates tag `vX.Y.Z` (`create-tag-on-merge.yml`), which triggers
  `publish.yml`: build, pack, push to NuGet.org via OIDC, and create a GitHub release.
  The tag must match `<Version>`.
- SemVer: breaking public API → major; new features or dependency updates → minor; fixes → patch.
- Version format must be plain `MAJOR.MINOR.PATCH` (no suffix) — the workflows reject anything else.
- Commit messages: conventional style appreciated (`chore(deps): ...`, `feat: ...`, `fix: ...`).

## Things to avoid

- Don't commit `nupkg/`, `bin/`, `obj/`, `.idea/` or `*.user` files.
- Don't change public API without updating `README.md` and bumping the version appropriately.
- Don't remove the obsolete `OsiLicensesClient` without a major version bump.
