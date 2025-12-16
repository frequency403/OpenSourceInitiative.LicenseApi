using System.Net;

namespace OpenSourceInitiative.LicenseApi.Tests.Utils;

public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder ?? throw new ArgumentNullException(nameof(responder));

    public int TotalCalls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        TotalCalls++;
        return Task.FromResult(_responder(request));
    }

    public static HttpResponseMessage JsonNdjson(string content)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/x-ndjson")
        };

    public static HttpResponseMessage Html(string html)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
        };

    public static HttpResponseMessage Status(HttpStatusCode status)
        => new(status);
}
