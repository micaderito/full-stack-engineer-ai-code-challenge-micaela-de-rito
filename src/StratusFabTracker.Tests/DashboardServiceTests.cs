using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

public class DashboardServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);
    private static DateOnly Today => DateOnly.FromDateTime(_now.UtcDateTime);

    private static DashboardService Build(IEnumerable<Spool>? spools = null)
        => new(new FakeSpoolRepository(spools), new FakeClock(_now));

    [Fact]
    public async Task GetDashboardAsync_returns_all_stations_in_dictionary()
    {
        var dto = await Build().GetDashboardAsync();

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
                new StatusEvent(Station.Weld, _now, "system")
            ])
        };

        var dto = await Build(spools).GetDashboardAsync();

        Assert.Equal(2, dto.WipByStation[Station.Detailing.ToString()]);
        Assert.Equal(1, dto.WipByStation[Station.Weld.ToString()]);
        Assert.Equal(0, dto.WipByStation[Station.Cut.ToString()]);
    }

    [Fact]
    public async Task GetDashboardAsync_spool_due_today_is_not_past_due()
    {
        var today = Today;
        var spool = SpoolFactory.Create("S1", dueDate: today);

        var dto = await Build([spool]).GetDashboardAsync();

        Assert.Equal(0, dto.PastDueCount);
    }

    [Fact]
    public async Task GetDashboardAsync_counts_past_due_non_installed_spools_and_excludes_installed()
    {
        var yesterday = Today.AddDays(-1);
        var tomorrow = Today.AddDays(1);

        var spools = new[]
        {
            SpoolFactory.Create("S1", dueDate: yesterday),                    // past due, Detailing → counts
            SpoolFactory.Create("S2", dueDate: yesterday, history:
            [
                new StatusEvent(Station.Installed, _now, "system")
            ]),                                                               // past due but Installed → excluded
            SpoolFactory.Create("S3", dueDate: tomorrow),                     // not past due → excluded
        };

        var dto = await Build(spools).GetDashboardAsync();

        Assert.Equal(1, dto.PastDueCount);
    }
}
