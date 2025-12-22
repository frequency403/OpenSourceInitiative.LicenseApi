using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Tests.Utils;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class ServiceCollectionHeadersTests
{
    [Fact]
    public async Task AddOsiLicensesClient_Sends_Default_Accept_And_UserAgent_Headers()
    {
        // Arrange
        ProductInfoHeaderValue[]? capturedUa = null;
        MediaTypeWithQualityHeaderValue[]? capturedAccept = null;
        string? firstApiUri = null;

        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            // Capture headers and the first API call to the license endpoint
            if (firstApiUri is null && uri == "https://opensource.org/api/license")
            {
                firstApiUri = uri;
                capturedAccept = req.Headers.Accept.ToArray();
                capturedUa = req.Headers.UserAgent.ToArray();

                const string json =
                    "[{'id':'mit','name':'MIT','spdx_id':'MIT','_links':{'self':{'href':'s'},'html':{'href':'https://opensource.org/license/mit/'},'collection':{'href':'c'}}}]";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json.Replace('\'', '"'), Encoding.UTF8, "application/json")
                };
            }

            if (uri.Contains("/license/mit/"))
                return StubHttpMessageHandler.Html("<div class='license-content'>MIT</div>");

            return StubHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOsiLicensesClient(o => o.PrimaryHandlerFactory = () => handler);

        await using var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IOsiLicensesClient>();

        // Act
        var licenses = await client.GetAllLicensesAsync();

        // Assert
        licenses.Should().ContainSingle();
        firstApiUri.Should().Be("https://opensource.org/api/license");
        capturedAccept.Should().NotBeNull();
        capturedAccept!.Should().Contain(x => x.MediaType == "application/json");
        capturedUa.Should().NotBeNull();
        capturedUa!.Should()
            .Contain(x => x.Product != null && x.Product.Name == "OpenSourceInitiative-LicenseApi-Client");
    }
}