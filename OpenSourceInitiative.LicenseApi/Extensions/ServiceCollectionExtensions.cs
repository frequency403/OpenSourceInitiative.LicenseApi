using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Caches;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Options;

namespace OpenSourceInitiative.LicenseApi.Extensions;

public static class ServiceCollectionExtensions
{
    private const string OsiClientName = nameof(OsiClient);
    internal const string OsiClientNonCachingName = "OsiNonCachingClient";

    /// <summary>
    ///     Registers <see cref="OpenSourceInitiative.LicenseApi.Interfaces.IOsiClient" /> as a typed client using <see cref="System.Net.Http.IHttpClientFactory" />.
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

        // 1. Configure HttpClient for the underlying OsiClient
        var clientBuilder = services.AddHttpClient(OsiClientName, client =>
        {
            client.ConfigureForLicenseApi(options);
        });

        if (options.PrimaryHandlerFactory is not null)
        {
            clientBuilder.ConfigurePrimaryHttpMessageHandler(_ => options.PrimaryHandlerFactory());
        }

        // 2. Register the appropriate IOsiClient
        if (options.EnableCaching)
        {
            services.AddKeyedSingleton<IOsiClient, OsiClient>(OsiClientNonCachingName, (sp, _) =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient(OsiClientName);
                return new OsiClient(sp.GetService<ILogger<OsiClient>>(), options, httpClient);
            });
            services.TryAddSingleton<ILicenseCache, AutoDetectCache>();

            services.AddSingleton<IOsiClient, OsiCachingClient>();
        }
        else
        {
            services.AddTransient<IOsiClient, OsiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient(OsiClientName);
                return new OsiClient(sp.GetService<ILogger<OsiClient>>(), options, httpClient);
            });
        }

        return services;
    }
}