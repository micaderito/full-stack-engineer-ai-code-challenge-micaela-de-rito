using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

public class DashboardServiceTests
{
    private static DashboardService Build(IEnumerable<Spool>? spools = null)
        => new(new FakeSpoolRepository(spools));

    [Fact]
    public async Task GetDashboardAsync_returns_all_stations_in_dictionary()
    {
        var svc = Build();
        var dto = await svc.GetDashboardAsync();

        foreach (var station in Enum.GetValues<Station>())
            Assert.True(dto.WipByStation.ContainsKey(station.ToString()));
    }

    [Fact]
    public async Task GetDashboardAsync_counts_zero_when_no_spools()
    {
        var dto = await Build().GetDashboardAsync();

        Assert.All(dto.WipByStation.Values, v => Assert.Equal(0, v));
        Assert.Equal(0, dto.PastDueCount);
    }

    [Fact]
    public async Task GetDashboardAsync_counts_spools_at_correct_station()
    {
        var spools = new[]
        {
            SpoolFactory.Create("S1"), // no history → Detailing
            SpoolFactory.Create("S2"), // no history → Detailing
            SpoolFactory.Create("S3", history:
            [
                new StatusEvent(Station.Weld, DateTimeOffset.UtcNow, "system")
            ])
        };

        var dto = await Build(spools).GetDashboardAsync();

        Assert.Equal(2, dto.WipByStation[Station.Detailing.ToString()]);
        Assert.Equal(1, dto.WipByStation[Station.Weld.ToString()]);
        Assert.Equal(0, dto.WipByStation[Station.Cut.ToString()]);
    }

    [Fact]
    public async Task GetDashboardAsync_counts_past_due_non_installed_spools()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var spools = new[]
        {
            SpoolFactory.Create("S1", dueDate: yesterday), // past due, Detailing
            SpoolFactory.Create("S2", dueDate: yesterday, history:
            [
                new StatusEvent(Station.Installed, DateTimeOffset.UtcNow, "system")
            ]), // past due but Installed — should NOT count
            SpoolFactory.Create("S3", dueDate: tomorrow), // not past due
        };

        var dto = await Build(spools).GetDashboardAsync();

        Assert.Equal(1, dto.PastDueCount);
    }

    [Fact]
    public async Task GetDashboardAsync_installed_spools_not_counted_as_past_due()
    {
        var pastDue = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        var spool = SpoolFactory.Create(dueDate: pastDue, history:
        [
            new StatusEvent(Station.Installed, DateTimeOffset.UtcNow, "system")
        ]);

        var dto = await Build([spool]).GetDashboardAsync();

        Assert.Equal(0, dto.PastDueCount);
    }
}
