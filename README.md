# OpenSourceInitiative.LicenseApi
[![CI](https://github.com/frequency403/OpenSourceInitiative.LicenseApi/actions/workflows/ci.yml/badge.svg)](https://github.com/frequency403/OpenSourceInitiative.LicenseApi/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) ![NuGet Version](https://img.shields.io/nuget/v/OpenSourceInitiative.LicenseApi?style=flat)

A lightweight, multi-targeted (.NET 10 / netstandard2.0) client library for the [Open Source Initiative License API](https://opensource.org/api). Fetches the OSI license catalog, supports filtering by SPDX ID, name, keyword, and steward, and extracts human-readable license text from the OSI HTML pages. No caching layer — every call goes to the network; callers that want caching add their own decorator.

---

## Quick start

### DI registration (recommended)

```csharp
services.AddOsiLicensesClient();

// With options
services.AddOsiLicensesClient(options =>
{
    options.BaseAddress = new Uri("https://...");   // default: OSI API
});
```

Resolve `IOsiClient` from the container:

```csharp
var client = sp.GetRequiredService<IOsiClient>();

// Stream all licenses
await foreach (var license in client.GetAllLicensesAsyncEnumerable())
    Console.WriteLine(license?.Name);

// Filter
var gplFamily = await client.GetBySpdxIdAsync("GPL*");
var popular   = await client.GetByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity);
```

### Standalone (no DI)

```csharp
await using var client = new OsiClient();
var mit = await client.GetByOsiIdAsync("mit");
Console.WriteLine(mit?.LicenseText);
```

---

## Public API surface

### `IOsiClient`

The primary interface for all license queries. Lives at `OpenSourceInitiative.LicenseApi.Interfaces`.

| Method                                 | Description                                                       |
|----------------------------------------|-------------------------------------------------------------------|
| `GetAllLicensesAsyncEnumerable(token)` | Streams the full catalog as `IAsyncEnumerable<OsiLicense?>`.      |
| `GetByOsiIdAsync(id, token)`           | Fetches a single license by its OSI ID (e.g. `"mit"`).            |
| `GetBySpdxIdAsync(id, token)`          | Filters by SPDX ID; supports `*` wildcards (`"GPL*"`, `"*-2.0"`). |
| `GetByNameAsync(name, token)`          | Filters by human-readable name.                                   |
| `GetByKeywordAsync(keyword, token)`    | Filters by `OsiLicenseKeyword` enum value.                        |
| `GetByStewardAsync(steward, token)`    | Filters by steward organization slug.                             |

Implements `IDisposable` and `IAsyncDisposable`.

> **Input handling:** `id`/`name`/`steward`/`keyword` arguments are always treated as opaque values and escaped before being combined with the configured `BaseAddress` — a caller passing a full URL, a `//host` reference, or `..` segments through one of these methods cannot redirect the request to a different host. Don't rely on this as input validation, though: garbage in still means "license not found" or a `JsonException`, not a meaningful error.

---

### `OsiClient`

`public sealed class` — the concrete HTTP implementation of `IOsiClient`. Registered as a typed `IHttpClientFactory` client named `"OsiClient"` when using DI.

Constructor: `OsiClient(ILogger<OsiClient>? logger, OsiClientOptions? options, HttpClient? httpClient)` — all parameters optional. When `httpClient` is `null`, the client owns and disposes the inner `HttpClient`; when provided externally it is not disposed.

License text is fetched automatically per license via a two-step strategy:
1. OSI HTML page — extracts the node with CSS class `license-content`.
2. Steward HTML URL — last resort, only used if the steward URL doesn't already point at a `.txt` file.

If both steps fail (network error, unreachable page, markup without a `license-content` node), `LicenseText` is left as an empty string rather than throwing — a scraping failure on a single license does not fail the whole call.

---

### `OsiClientOptions`

`public sealed class` — configures the DI registration. Lives at `OpenSourceInitiative.LicenseApi.Options`.

| Property                | Type                            | Default                       | Description                                            |
|-------------------------|---------------------------------|-------------------------------|--------------------------------------------------------|
| `BaseAddress`           | `Uri`                           | `https://opensource.org/api/` | OSI API base URL.                                      |
| `PrimaryHandlerFactory` | `Func<HttpMessageHandler>?`     | `null`                        | Injects a custom primary handler (useful for testing). |
| `UserAgent`             | `IList<ProductInfoHeaderValue>` | Assembly name + version       | Added to every request.                                |
| `HttpClientHandler`     | `HttpClientHandler`             | `AllowAutoRedirect = true`    | Used when no external `HttpClient` is supplied.        |

---

### `OsiLicense`

`public sealed record` — represents one OSI license entry. Lives at `OpenSourceInitiative.LicenseApi.Models`.

| Property         | Type                                     | Notes                                                                                                                                                                                                                       |
|------------------|------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Id`             | `string`                                 | OSI-internal identifier (e.g. `"mit"`).                                                                                                                                                                                     |
| `Name`           | `string`                                 | Human-readable name.                                                                                                                                                                                                        |
| `SpdxId`         | `string?`                                | SPDX identifier (e.g. `"MIT"`).                                                                                                                                                                                             |
| `Version`        | `string?`                                | Optional version string.                                                                                                                                                                                                    |
| `SubmissionDate` | `DateTime?`                              | Parsed from `yyyyMMdd` via `CustomFormatDateTimeConverter`.                                                                                                                                                                 |
| `ApprovalDate`   | `DateTime?`                              | Parsed from `yyyyMMdd`.                                                                                                                                                                                                     |
| `Approved`       | `bool`                                   | OSI approval status.                                                                                                                                                                                                        |
| `Keywords`       | `IReadOnlyCollection<OsiLicenseKeyword>` | Deserialized via `OsiLicenseKeywordsConverter`; unknown tokens are ignored.                                                                                                                                                 |
| `Stewards`       | `IReadOnlyCollection<string>`            | Steward organization slugs.                                                                                                                                                                                                 |
| `Links`          | `OsiLicenseLinks`                        | HAL-style `_links` object.                                                                                                                                                                                                  |
| `LicenseText`    | `string?`                                | Extracted plain text — not part of the API payload, populated by the client after fetching. Empty string if scraping failed. Mutable, so you can set it yourself when constructing an `OsiLicense` by hand (e.g. in tests). |

`OsiLicenseExtensions` adds `license.GetLicenseText(httpClient, token)` and `license.GetAndSetLicenseText(httpClient, token)` for re-fetching or backfilling the text on an existing instance.

---

### `OsiLicenseKeyword`

`public enum` — OSI classification keywords. Lives at `OpenSourceInitiative.LicenseApi.Enums`.

Values: `PopularStrongCommunity`, `International`, `SpecialPurpose`, `NonReusable`, `Superseded`, `VoluntarilyRetired`, `RedundantWithMorePopular`, `OtherMiscellaneous`, `Uncategorized`.

Serializes to/from the OSI API string tokens (e.g. `popular-strong-community`) via `OsiLicenseKeywordsConverter`.

---

### `ServiceCollectionExtensions`

`public static class` — the single registration entry point. Lives at `OpenSourceInitiative.LicenseApi.Extensions`.

`AddOsiLicensesClient(this IServiceCollection, Action<OsiClientOptions>?)` — registers a named `IHttpClientFactory` client (`"OsiClient"`) and `IOsiClient` → `OsiClient` as transient.

---

### `OsiException` / `OsiInitializationException`

`public abstract/sealed class` — base and derived exception types. Live at `OpenSourceInitiative.LicenseApi.Exceptions`.

`OsiInitializationException` is thrown by the obsolete `OsiLicensesClient.InitializeAsync` when the catalog fetch fails.

---

### `CustomFormatDateTimeConverter`

`public class JsonConverter<DateTime?>` — handles the `yyyyMMdd` date format used by the OSI API for submission and approval dates. Registered via `[JsonConverter]` attributes on `OsiLicense`. Null and empty strings deserialize to `null`; an unrecognized format throws `JsonException`.

---

### `IOsiLicensesClient` / `OsiLicensesClient` *(deprecated)*

`[Obsolete]` — wraps `IOsiClient` with an eager-loading, snapshot-based API kept for backward compatibility. Use `IOsiClient` directly for new code. Unlike `IOsiClient`, its `SearchAsync`/`Search` assume every entry returned by the API has a non-null `Id`/`Name`; a malformed catalog entry missing either field will throw a `NullReferenceException` here rather than being filtered out.

---

## Target frameworks

| TFM              | Notes                                                                                                                           |
|------------------|---------------------------------------------------------------------------------------------------------------------------------|
| `net10.0`        | Full feature set. Uses `ValueTask.CompletedTask`, `MediaTypeNames`, `ReadAsStreamAsync(token)`, C# 14 extension blocks.         |
| `netstandard2.0` | Compatible subset. `#if !NETSTANDARD2_0` guards cover API surface differences. `System.Text.Json` pulled as a NuGet dependency. |
