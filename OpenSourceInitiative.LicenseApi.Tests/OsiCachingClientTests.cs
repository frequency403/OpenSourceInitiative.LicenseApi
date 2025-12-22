using System.Net;
using System.Text;
using OpenSourceInitiative.LicenseApi.Clients;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using OpenSourceInitiative.LicenseApi.Tests.Utils;
using FluentAssertions;
using Xunit;

namespace OpenSourceInitiative.LicenseApi.Tests;

public class OsiCachingClientTests
{
    private static (IOsiClient osiClient, StubHttpMessageHandler handler) CreateOsiClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler);
        return (new OsiClient(httpClient: http), handler);
    }

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source) list.Add(item);
        return list;
    }

    [Fact]
    public async Task GetAllLicensesAsyncEnumerable_CachesResults()
    {
        // Arrange
        var (baseClient, handler) = CreateOsiClient(req =>
        {
            const string json = "[{\"id\":\"mit\",\"name\":\"MIT\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}}]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var cachingClient = new OsiCachingClient(baseClient);

        // Act
        var first = await ToListAsync(cachingClient.GetAllLicensesAsyncEnumerable());
        var second = await ToListAsync(cachingClient.GetAllLicensesAsyncEnumerable());

        // Assert
        handler.TotalCalls.Should().Be(1);
        first.Should().HaveCount(1);
        second.Should().HaveCount(1);
        second[0].Should().BeSameAs(first[0]);
    }

    [Fact]
    public async Task GetByOsiIdAsync_CachesResults()
    {
        // Arrange
        var (baseClient, handler) = CreateOsiClient(req =>
        {
            const string json = "{\"id\":\"mit\",\"name\":\"MIT\",\"spdx_id\":\"MIT\",\"_links\":{\"self\":{\"href\":\"s\"},\"html\":{\"href\":\"h\"},\"collection\":{\"href\":\"c\"}}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var cachingClient = new OsiCachingClient(baseClient);

        // Act
        var first = await cachingClient.GetByOsiIdAsync("mit");
        var second = await cachingClient.GetByOsiIdAsync("mit");

        // Assert
        handler.TotalCalls.Should().Be(1);
        first.Should().NotBeNull();
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task OtherQueryMethods_CachesResults()
    {
        // Arrange
        var (baseClient, handler) = CreateOsiClient(req =>
        {
            const string json = "[]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var cachingClient = new OsiCachingClient(baseClient);

        // Act
        await cachingClient.GetBySpdxIdAsync("MIT");
        await cachingClient.GetBySpdxIdAsync("MIT");
        await cachingClient.GetByNameAsync("Apache");
        await cachingClient.GetByNameAsync("Apache");
        await cachingClient.GetByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity);
        await cachingClient.GetByKeywordAsync(OsiLicenseKeyword.PopularStrongCommunity);
        await cachingClient.GetByStewardAsync("Eclipse");
        await cachingClient.GetByStewardAsync("Eclipse");

        // Assert
        handler.TotalCalls.Should().Be(4);
    }
}
