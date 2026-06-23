# OpenSourceInitiative.LicenseApi 
[![CI](https://github.com/frequency403/OpenSourceInitiative.LicenseApi/actions/workflows/ci.yml/badge.svg)](https://github.com/frequency403/OpenSourceInitiative.LicenseApi/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) ![NuGet Version](https://img.shields.io/nuget/v/OpenSourceInitiative.LicenseApi?style=flat)


A lightweight, multi-targeted (.NET 10 / netstandard2.0) client library for the [Open Source Initiative License API](https://opensource.org/api). Fetches the full OSI license catalog, supports filtering by SPDX ID, name, keyword, and steward, and extracts human-readable license text from the OSI HTML pages. Includes an optional transparent caching layer that auto-detects your registered `IDistributedCache` or `IMemoryCache`, with a `ConcurrentDictionary`-based fallback requiring zero configuration.

---

## Quick start

### DI registration (recommended)

```csharp
// Minimal — caching on by default, fallback in-memory cache
services.AddOsiLicensesClient();

// With options
services.AddOsiLicensesClient(options =>
{
    options.EnableCaching = true;                        // default: true
    options.BaseAddress   = new Uri("https://...");      // default: OSI API
});

// With distributed cache — AutoDetectCache picks it up automatically
services.AddStackExchangeRedisCache(...);
services.AddOsiLicensesClient();
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

---

### `OsiClient`

`public sealed class` — the concrete HTTP implementation of `IOsiClient`. Registered as a typed `IHttpClientFactory` client named `"OsiClient"` when using DI.

Constructor: `OsiClient(ILogger<OsiClient>? logger, OsiClientOptions? options, HttpClient? httpClient)` — all parameters optional. When `httpClient` is `null`, the client owns and disposes the inner `HttpClient`; when provided externally it is not disposed.

License text is fetched automatically per license via a three-step strategy:
1. Steward `.txt` URL (authoritative plain text, no parsing).
2. OSI HTML page — extracts the node with CSS class `license-content`.
3. Steward HTML URL — last resort HTML extraction.

---

### `OsiClientOptions`

`public sealed class` — configures the DI registration. Lives at `OpenSourceInitiative.LicenseApi.Options`.

| Property                | Type                            | Default                       | Description                                            |
|-------------------------|---------------------------------|-------------------------------|--------------------------------------------------------|
| `BaseAddress`           | `Uri`                           | `https://opensource.org/api/` | OSI API base URL.                                      |
| `EnableCaching`         | `bool`                          | `true`                        | Wraps the registered client with `OsiCachingClient`.   |
| `PrimaryHandlerFactory` | `Func<HttpMessageHandler>?`     | `null`                        | Injects a custom primary handler (useful for testing). |
| `UserAgent`             | `IList<ProductInfoHeaderValue>` | Assembly name + version       | Added to every request.                                |
| `HttpClientHandler`     | `HttpClientHandler`             | `AllowAutoRedirect = true`    | Used when no external `HttpClient` is supplied.        |

---

### `OsiLicense`

`public sealed record` — represents one OSI license entry. Lives at `OpenSourceInitiative.LicenseApi.Models`.

| Property         | Type                                     | Notes                                                                       |
|------------------|------------------------------------------|-----------------------------------------------------------------------------|
| `Id`             | `string`                                 | OSI-internal identifier (e.g. `"mit"`).                                     |
| `Name`           | `string`                                 | Human-readable name.                                                        |
| `SpdxId`         | `string?`                                | SPDX identifier (e.g. `"MIT"`).                                             |
| `Version`        | `string?`                                | Optional version string.                                                    |
| `SubmissionDate` | `DateTime?`                              | Parsed from `yyyyMMdd` via `CustomFormatDateTimeConverter`.                 |
| `ApprovalDate`   | `DateTime?`                              | Parsed from `yyyyMMdd`.                                                     |
| `Approved`       | `bool`                                   | OSI approval status.                                                        |
| `Keywords`       | `IReadOnlyCollection<OsiLicenseKeyword>` | Deserialized via `OsiLicenseKeywordsConverter`; unknown tokens are ignored. |
| `Stewards`       | `IReadOnlyCollection<string>`            | Steward organization slugs.                                                 |
| `Links`          | `OsiLicenseLinks`                        | HAL-style `_links` object.                                                  |
| `LicenseText`    | `string`                                 | Extracted plain text — not part of the API payload (`[JsonIgnore]`).        |

---

### `OsiLicenseKeyword`

`public enum` — OSI classification keywords. Lives at `OpenSourceInitiative.LicenseApi.Enums`.

Values: `PopularStrongCommunity`, `International`, `SpecialPurpose`, `NonReusable`, `Superseded`, `VoluntarilyRetired`, `RedundantWithMorePopular`, `OtherMiscellaneous`, `Uncategorized`.

Serializes to/from the OSI API string tokens (e.g. `popular-strong-community`) via `OsiLicenseKeywordsConverter`.

---

### `ServiceCollectionExtensions`

`public static class` — the single registration entry point. Lives at `OpenSourceInitiative.LicenseApi.Extensions`.

`AddOsiLicensesClient(this IServiceCollection, Action<OsiClientOptions>?)` — registers:
- A named `IHttpClientFactory` client (`"OsiClient"`).
- `IOsiClient` → `OsiClient` (non-caching, registered as a keyed singleton `"OsiNonCachingClient"` when caching is on, or as a transient when off).
- When `EnableCaching = true`: `ILicenseCache` → `AutoDetectCache` (via `TryAddSingleton`) and `IOsiClient` → `OsiCachingClient` (singleton).

---

### `OsiException` / `OsiInitializationException`

`public abstract/sealed class` — base and derived exception types. Live at `OpenSourceInitiative.LicenseApi.Exceptions`.

`OsiInitializationException` is thrown by `OsiLicensesClient.InitializeAsync` when the catalog fetch fails.

---

### `CustomFormatDateTimeConverter`

`public class JsonConverter<DateTime?>` — handles the `yyyyMMdd` date format used by the OSI API for submission and approval dates. Registered via `[JsonConverter]` attributes on `OsiLicense`. Null and empty strings deserialize to `null`; an unrecognized format throws `JsonException`.

---

### `IOsiLicensesClient` / `OsiLicensesClient` *(deprecated)*

`[Obsolete]` — wraps `IOsiClient` with an eager-loading, snapshot-based API for backward compatibility. Use `IOsiClient` directly for new code.

---

## Internal architecture

| Type                       | Visibility        | Role                                                                                                                                                                                               |
|----------------------------|-------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `OsiCachingClient`         | `internal sealed` | Decorator over `IOsiClient` — intercepts all queries, checks cache before delegating.                                                                                                              |
| `ILicenseCache`            | `internal`        | Uniform async cache contract: `GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`.                                                                                                                         |
| `AutoDetectCache`          | `internal sealed` | Selects the appropriate `ILicenseCache` implementation at construction: `IDistributedCache` → `DistributedCacheAdapter`; `IMemoryCache` → `MemoryCacheAdapter`; neither → `InMemoryCacheFallback`. |
| `DistributedCacheAdapter`  | `internal`        | Adapts `IDistributedCache` to `ILicenseCache` using `System.Text.Json` serialization.                                                                                                              |
| `MemoryCacheAdapter`       | `internal`        | Adapts `IMemoryCache` to `ILicenseCache`.                                                                                                                                                          |
| `InMemoryCacheFallback`    | `internal`        | Thread-safe `ConcurrentDictionary`-based cache with optional TTL expiry.                                                                                                                           |
| `HttpClientExtensions`     | `internal static` | `GetLicenseTextAsync`, `TryFetchPlainTextAsync`, `TryFetchHtmlLicenseTextAsync`, `ConfigureForLicenseApi`.                                                                                         |
| `OsiLicenseKeywordMapping` | `internal static` | Bidirectional map between `OsiLicenseKeyword` enum values and API string tokens.                                                                                                                   |

---

## Cache selection matrix

```
IDistributedCache registered?  ──Yes──▶  DistributedCacheAdapter
        │ No
        ▼
IMemoryCache registered?  ──Yes──▶  MemoryCacheAdapter
        │ No
        ▼
InMemoryCacheFallback  (zero-config, process-local)
```

The selection is performed once at `AutoDetectCache` construction — no runtime switching. `TryAddSingleton` ensures consumer-registered `ILicenseCache` implementations take precedence.

---

## Target frameworks

| TFM              | Notes                                                                                                                           |
|------------------|---------------------------------------------------------------------------------------------------------------------------------|
| `net10.0`        | Full feature set. Uses `ValueTask.CompletedTask`, `MediaTypeNames`, `ReadAsStreamAsync(token)`, C# 14 extension blocks.         |
| `netstandard2.0` | Compatible subset. `#if !NETSTANDARD2_0` guards cover API surface differences. `System.Text.Json` pulled as a NuGet dependency. |