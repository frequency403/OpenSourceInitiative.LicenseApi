using System.Net.Sockets;
using System.Runtime.CompilerServices;

// ReSharper disable ExplicitCallerInfoArgument

namespace OpenSourceInitiative.LicenseApi.Tests.Infrastructure;

/// <summary>
///     xUnit Fact that is automatically skipped when the live OSI API is unreachable.
/// </summary>
/// <remarks>
///     <br />- Set environment variable OSI_API_TESTS=1 to force tests to run without reachability probe.
///     <br />- The reachability probe uses a short timeout to avoid delaying other test runs.
/// </remarks>
public sealed class OsiApiAvailableFactAttribute : FactAttribute
{
    private const string DefaultBaseUrl = "https://opensource.org/api/license";

    public OsiApiAvailableFactAttribute([CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0) : base(sourceFilePath, sourceLineNumber)
    {
        try
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("OSI_API_TESTS"), out var parsedInt) && parsedInt is 1)
                return;

            // Quick TCP reachability check on port 443 to avoid HTTP stack overhead
            var host = new Uri(DefaultBaseUrl).Host;
            using var tcp = new TcpClient();
            var result = tcp.BeginConnect(host, 443, null, null);
            if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(20)))
            {
                Skip = "Skipping: OSI API not reachable (timeout). Set OSI_API_TESTS=1 to force run.";
                return;
            }

            tcp.EndConnect(result);
        }
        catch
        {
            Skip = "Skipping: OSI API not reachable. Set OSI_API_TESTS=1 to force run.";
        }
    }
}