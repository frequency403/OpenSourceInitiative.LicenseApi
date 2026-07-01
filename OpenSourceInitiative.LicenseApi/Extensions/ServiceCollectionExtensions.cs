using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Options;

namespace OpenSourceInitiative.LicenseApi.Extensions;

public static class ServiceCollectionExtensions
{
    private const string OsiClientName = nameof(OsiClient);
    internal const string OsiClientNonCachingName = "OsiNonCachingClient";

    /// <summary>
    ///     Registers <see cref="OpenSourceInitiative.LicenseApi.Interfaces.IOsiClient" /> as a typed client using
    ///     <see cref="System.Net.Http.IHttpClientFactory" />.
    ///     Supports optional base address configuration and a custom primary handler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the OSI client registration.</param>
    /// <returns>The same <paramref name="services" /> instance for chaining.</returns>
    /// <remarks>
    ///     <br />- When no base address is provided, the OSI License API base address is used.
    ///     <br />- A custom <see cref="OsiClientOptions.PrimaryHandlerFactory" /> allows testability via in-memory handlers.
    /// </remarks>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection AddOsiLicensesClient(this IServiceCollection services,
        Action<OsiClientOptions>? configure = null)
    {
#if !NETSTANDARD2_0
        ArgumentNullException.ThrowIfNull(services);
#else
        if (services is null) throw new ArgumentNullException(nameof(services));
#endif

        var options = new OsiClientOptions();
        configure?.Invoke(options);

        var clientBuilder =
            services.AddHttpClient<OsiClient>(client => client.ConfigureForLicenseApi(options));

        if (options.PrimaryHandlerFactory is not null)
            clientBuilder.ConfigurePrimaryHttpMessageHandler(_ => options.PrimaryHandlerFactory());

        services.AddTransient<IOsiClient, OsiClient>();

        return services;
    }
}