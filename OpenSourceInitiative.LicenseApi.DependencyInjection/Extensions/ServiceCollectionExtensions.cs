using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.DependencyInjection.Http;
using OpenSourceInitiative.LicenseApi.DependencyInjection.Options;

namespace OpenSourceInitiative.LicenseApi.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IOsiLicensesClient"/> as a typed client using <see cref="IHttpClientFactory"/>.
    /// Supports optional base address configuration, a custom primary handler, and request/response logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the OSI client registration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <remarks>
    /// - When no base address is provided, the OSI License API base address is used.
    /// - When <see cref="OsiClientOptions.EnableLogging"/> is true, a lightweight logging handler is added.
    /// - A custom <see cref="OsiClientOptions.PrimaryHandlerFactory"/> allows testability via in-memory handlers.
    /// </remarks>
    public static IServiceCollection AddOsiLicensesClient(this IServiceCollection services, Action<OsiClientOptions>? configure = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var options = new OsiClientOptions();
        configure?.Invoke(options);

        var builder = services.AddHttpClient<IOsiLicensesClient, OsiLicensesClient>(client =>
        {
            if (client.BaseAddress is null)
            {
                client.BaseAddress = options.BaseAddress ?? new Uri("https://opensource.org/api/");
            }
        });

        if (options.PrimaryHandlerFactory is not null)
        {
            builder.ConfigurePrimaryHttpMessageHandler(_ => options.PrimaryHandlerFactory());
        }

        if (options.EnableLogging)
        {
            builder.AddHttpMessageHandler(sp => new LoggingHandler(sp.GetRequiredService<ILogger<LoggingHandler>>()));
        }

        return services;
    }
}
