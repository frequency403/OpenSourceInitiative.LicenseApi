using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OpenSourceInitiative.LicenseApi.DependencyInjection.Http;

/// <summary>
/// Delegating handler that logs outgoing requests and response results.
/// </summary>
/// <remarks>
/// - Logs method and URI at Information level before sending the request.
/// - Logs a Debug entry for successful responses and a Warning for non‑success status codes, including elapsed time.
/// - Logs errors at Error level when the underlying pipeline throws.
/// - Does not log payload bodies to avoid leaking sensitive data.
/// </remarks>
public sealed class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="LoggingHandler"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("HTTP {Method} {Uri}", request.Method, request.RequestUri);
        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            sw.Stop();
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("HTTP {StatusCode} in {Elapsed} ms for {Method} {Uri}", (int)response.StatusCode, sw.ElapsedMilliseconds, request.Method, request.RequestUri);
            }
            else
            {
                _logger.LogWarning("HTTP {StatusCode} in {Elapsed} ms for {Method} {Uri}", (int)response.StatusCode, sw.ElapsedMilliseconds, request.Method, request.RequestUri);
            }
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "HTTP failed in {Elapsed} ms for {Method} {Uri}", sw.ElapsedMilliseconds, request.Method, request.RequestUri);
            throw;
        }
    }
}
