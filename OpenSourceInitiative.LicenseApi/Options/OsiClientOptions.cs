// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
using System.Net.Http.Headers;
using System.Reflection;

namespace OpenSourceInitiative.LicenseApi.Options;

/// <summary>
///     Options to control the DI registration behavior for the OSI licenses client.
/// </summary>
public sealed class OsiClientOptions
{
    /// <summary>
    ///     The base address of the OSI API.
    /// </summary>
    private const string OpenSourceInitiativeLicenseApiBaseUrl = "https://opensource.org/api/";

    /// <summary>
    ///     Optional base address to use; defaults to the OSI API base URL.
    /// </summary>
    public Uri BaseAddress { get; set; } = new(OpenSourceInitiativeLicenseApiBaseUrl);

    /// <summary>
    ///     Allows tests/consumers to supply a custom primary HTTP message handler
    ///     (e.g., for mocking or adding custom sockets/policies).
    /// </summary>
    public Func<HttpMessageHandler>? PrimaryHandlerFactory { get; set; }

    /// <summary>
    ///     Specifies whether in-memory caching is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>
    /// User-Agent header to use for all requests when using the prepared <see cref="HttpClient"/> by this library.
    /// </summary>
    public IList<ProductInfoHeaderValue> UserAgent { get; set; } = new List<ProductInfoHeaderValue>()
    {
        { new(Assembly.GetExecutingAssembly().GetName().Name ?? "OpenSourceInitiative.LicenseApi", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty)}
    };
}