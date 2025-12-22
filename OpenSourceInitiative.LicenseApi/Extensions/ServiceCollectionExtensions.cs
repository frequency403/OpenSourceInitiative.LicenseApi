using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Options;

namespace OpenSourceInitiative.LicenseApi.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers <see cref="OpenSourceInitiative.LicenseApi.Interfaces.IOsiLicensesClient" /> as a typed client using <see cref="System.Net.Http.IHttpClientFactory" />.
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
        if (services is null) throw new ArgumentNullException(nameof(services));

        var options = new OsiClientOptions();
        configure?.Invoke(options);

        // 1. Configure HttpClient for the underlying OsiClient
        var clientBuilder = services.AddHttpClient("OsiClient", client =>
        {
            client.BaseAddress ??= options.BaseAddress;

            if (client.DefaultRequestHeaders.Accept.All(h => h.MediaType != "application/json"))
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (client.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("OpenSourceInitiative-LicenseApi-Client", version));
            }
        });

        if (options.PrimaryHandlerFactory is not null)
        {
            clientBuilder.ConfigurePrimaryHttpMessageHandler(_ => options.PrimaryHandlerFactory());
        }

        // 2. Register the appropriate IOsiClient
        if (options.EnableCaching)
        {
            services.AddKeyedSingleton<IOsiClient, OsiClient>("OsiNonCachingClient", (sp, _) =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("OsiClient");
                return new OsiClient(sp.GetService<ILogger<OsiClient>>(), options, httpClient);
            });

            services.AddSingleton<IOsiClient, OsiCachingClient>();
        }
        else
        {
            services.AddTransient<IOsiClient, OsiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("OsiClient");
                return new OsiClient(sp.GetService<ILogger<OsiClient>>(), options, httpClient);
            });
        }

        // 3. Register IOsiLicensesClient
        services.AddTransient<IOsiLicensesClient, OsiLicensesClient>();

        return services;
    }
}