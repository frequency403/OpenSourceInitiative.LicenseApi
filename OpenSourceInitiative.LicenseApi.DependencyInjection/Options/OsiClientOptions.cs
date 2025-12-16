namespace OpenSourceInitiative.LicenseApi.DependencyInjection.Options;

/// <summary>
/// Options to control the DI registration behavior for the OSI licenses client.
/// </summary>
public sealed class OsiClientOptions
{
    /// <summary>
    /// The base address of the OSI API.
    /// </summary>
    private const string OpenSourceInitiativeLicenseApiBaseUrl = "https://opensource.org/api/";
    
    /// <summary>
    /// Optional base address to use; defaults to the OSI API base URL.
    /// </summary>
    public Uri BaseAddress { get; set; } = new(OpenSourceInitiativeLicenseApiBaseUrl);

    /// <summary>
    /// Enables a simple logging delegating handler for requests and responses.
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Allows tests/consumers to supply a custom primary HTTP message handler
    /// (e.g., for mocking or adding custom sockets/policies).
    /// </summary>
    public Func<HttpMessageHandler>? PrimaryHandlerFactory { get; set; }
}
