using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class OsiClientTests
{
    private static (OsiClient osiClient, StubHttpMessageHandler handler) CreateOsiClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler);
        return (new OsiClient(httpClient: http), handler);
    }

    [Fact]
    public async Task GetByKeywordAsync_SendsCorrectRequest()
    {
        // Arrange
        var (client, handler) = CreateOsiClient(req =>
        {
            if (req.RequestUri!.ToString().Contains("keyword=popular-strong-community"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        // Act
        await client.GetByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity);

        // Assert
        handler.TotalCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetByStewardAsync_SendsCorrectRequest()
    {
        // Arrange
        var (client, handler) = CreateOsiClient(req =>
        {
            if (req.RequestUri!.ToString().Contains("steward=eclipse"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        // Act
        await client.GetByStewardAsync("eclipse");

        // Assert
        handler.TotalCalls.Should().Be(1);
    }
}
