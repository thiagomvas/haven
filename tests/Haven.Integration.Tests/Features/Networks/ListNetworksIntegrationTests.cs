using System.Net;
using System.Net.Http.Json;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Networks.Queries.ListNetworks;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Persistence;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Networks;

[TestFixture]
[Category("Integration")]
public class ListNetworksIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;
    private HavenDbContext _dbContext = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _dbContext = _fixture.GetService<HavenDbContext>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
        _dbContext.Dispose();
    }

    [Test]
    public async Task ListNetworks_ReturnsAllNetworks_WithNoFilter()
    {
        _dbContext.Networks.Add(Network.Create("shared-network", NetworkType.Shared));
        _dbContext.Networks.Add(Network.Create("external-network", NetworkType.External));
        await _dbContext.SaveChangesAsync();

        var response = await _fixture.Client.GetAsync("/api/networks");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<NetworkDto>>>();
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data.Select(n => n.Name).ShouldBe(["external-network", "shared-network"], ignoreOrder: true);
    }

    [Test]
    public async Task ListNetworks_FiltersByType()
    {
        _dbContext.Networks.Add(Network.Create("shared-network", NetworkType.Shared));
        _dbContext.Networks.Add(Network.Create("external-network", NetworkType.External));
        await _dbContext.SaveChangesAsync();

        var response = await _fixture.Client.GetAsync("/api/networks?type=Shared");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<NetworkDto>>>();
        result.ShouldNotBeNull();
        result.Data.ShouldNotBeNull();
        result.Data.Single().Name.ShouldBe("shared-network");
    }
}
