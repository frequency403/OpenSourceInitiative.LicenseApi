using OpenSourceInitiative.LicenseApi.Models;

namespace OpenSourceInitiative.LicenseApi.Extensions;

public static class OsiLicenseExtensions
{
    /// <summary>
    /// Provides extension members for <see cref="OsiLicense"/> instances to retrieve
    /// and populate their associated license text via HTTP.
    /// </summary>
    extension(OsiLicense license)
    {
        /// <summary>
        /// Retrieves the full license text for this <see cref="OsiLicense"/> from the OSI API.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> used to perform the request.</param>
        /// <param name="token">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that resolves to the license text as a string.</returns>
        public Task<string> GetLicenseText(HttpClient client, CancellationToken token = default)
            => client.GetLicenseTextAsync(license, token);

        /// <summary>
        /// Retrieves the license text for this <see cref="OsiLicense"/> and assigns it
        /// directly to <see cref="OsiLicense.LicenseText"/>.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> used to perform the request.</param>
        /// <param name="token">A token to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous set operation.</returns>
        public async Task GetAndSetLicenseText(HttpClient client, CancellationToken token = default)
            => license.LicenseText = await license.GetLicenseText(client, token);
    }
}