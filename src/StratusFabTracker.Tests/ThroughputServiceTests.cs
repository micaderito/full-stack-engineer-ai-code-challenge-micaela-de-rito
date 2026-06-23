using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

public class ThroughputServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);
    private static DateOnly Today => DateOnly.FromDateTime(_now.UtcDateTime);

    private ThroughputService Build(IEnumerable<Spool>? spools = null)
        => new(new FakeSpoolRepository(spools), new FakeClock(_now));

    [Fact]
    public async Task GetThroughputAsync_returns_14_daily_entries()
    {
        var dto = await Build().GetThroughputAsync();
        Assert.Equal(14, dto.Daily.Count);
    }

    [Fact]
    public async Task GetThroughputAsync_window_starts_13_days_ago()
    {
        var dto = await Build().GetThroughputAsync();
        Assert.Equal(Today.AddDays(-13), dto.Daily.First().Day);
        Assert.Equal(Today, dto.Daily.Last().Day);
    }

    [Fact]
    public async Task GetThroughputAsync_counts_installed_events_in_window()
    {
        var installedAt = _now.AddDays(-3); // within 14-day window
        var spool = SpoolFactory.Create(history:
        [
            new StatusEvent(Station.Installed, installedAt, "system")
        ]);

        var dto = await Build([spool]).GetThroughputAsync();

        var day = dto.Daily.Single(d => d.Day == DateOnly.FromDateTime(installedAt.UtcDateTime));
        Assert.Equal(1, day.Completed);
    }

    [Fact]
    public async Task GetThroughputAsync_excludes_installed_events_outside_window()
    {
        var outsideWindow = _now.AddDays(-20);
        var spool = SpoolFactory.Create(history:
        [
            new StatusEvent(Station.Installed, outsideWindow, "system")
        ]);

        var dto = await Build([spool]).GetThroughputAsync();

        Assert.All(dto.Daily, d => Assert.Equal(0, d.Completed));
    }

    [Fact]
    public async Task GetThroughputAsync_excludes_non_installed_status_events()
    {
        var spool = SpoolFactory.Create(history:
        [
            new StatusEvent(Station.Weld, _now.AddDays(-1), "system")
        ]);

        var dto = await Build([spool]).GetThroughputAsync();

        Assert.All(dto.Daily, d => Assert.Equal(0, d.Completed));
    }

    [Fact]
    public async Task GetThroughputAsync_keeping_up_true_when_completions_exceed_due()
    {
        // 14 installs in window, 0 spools due in window → completedPerDay > duePerDay
        var spools = Enumerable.Range(0, 14).Select(i =>
            SpoolFactory.Create(
                id: $"S{i}",
                dueDate: Today.AddDays(60), // due far in future, outside window
                history: [new StatusEvent(Station.Installed, _now.AddDays(-i % 14), "system")]
            )).ToArray();

        var dto = await Build(spools).GetThroughputAsync();

        Assert.True(dto.KeepingUp);
    }

    [Fact]
    public async Task GetThroughputAsync_keeping_up_false_when_completions_below_due()
    {
        // No installs but many spools due in window
        var spools = Enumerable.Range(0, 28).Select(i =>
            SpoolFactory.Create(
                id: $"S{i}",
                dueDate: Today.AddDays(-(i % 14)) // all due within window
            )).ToArray();

        var dto = await Build(spools).GetThroughputAsync();

        Assert.False(dto.KeepingUp);
    }

    [Fact]
    public async Task GetThroughputAsync_completed_per_day_is_average_of_daily()
    {
        var dto = await Build().GetThroughputAsync();
        var expected = dto.Daily.Average(d => d.Completed);
        Assert.Equal(expected, dto.CompletedPerDay);
    }
}
