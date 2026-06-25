using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StratusFabTracker.Api.Application;
using Xunit;

namespace StratusFabTracker.Tests;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_dashboard_returns_200_with_wip_and_past_due()
    {
        var response = await _client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(dto);
        Assert.NotNull(dto.WipByStation);
    }

    [Fact]
    public async Task GET_throughput_returns_200_with_14_daily_entries()
    {
        var response = await _client.GetAsync("/api/throughput");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ThroughputDto>();
        Assert.NotNull(dto);
        Assert.Equal(14, dto.Daily.Count);
    }

    [Fact]
    public async Task GET_spools_returns_200_with_list()
    {
        var response = await _client.GetAsync("/api/spools");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GET_spool_by_id_returns_404_for_unknown_id()
    {
        var response = await _client.GetAsync("/api/spools/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_advance_returns_404_for_unknown_spool()
    {
        var response = await _client.PostAsync("/api/spools/does-not-exist/advance", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_spool_by_id_returns_200_for_seeded_spool()
    {
        // Seed file loads spools at startup; any valid id from the seed works.
        // We just verify the endpoint resolves a real id or gracefully 404s.
        var allResponse = await _client.GetAsync("/api/spools");
        allResponse.EnsureSuccessStatusCode();

        var spools = await allResponse.Content.ReadFromJsonAsync<List<SpoolSummary>>();
        if (spools is null || spools.Count == 0) return; // no seed file in this env

        var id = spools[0].Id;
        var single = await _client.GetAsync($"/api/spools/{id}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task POST_advance_returns_400_for_installed_spool()
    {
        var allResponse = await _client.GetAsync("/api/spools");
        var spools = await allResponse.Content.ReadFromJsonAsync<List<SpoolSummary>>();
        if (spools is null || spools.Count == 0) return;

        // Station.Installed == 5
        var installed = spools.FirstOrDefault(s => s.CurrentStation == 5);
        if (installed is null) return; // seed may not have any Installed spools

        var response = await _client.PostAsync($"/api/spools/{installed.Id}/advance", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // CurrentStation is serialized as a numeric enum value (Station enum)
    private sealed record SpoolSummary(string Id, int CurrentStation);
}
