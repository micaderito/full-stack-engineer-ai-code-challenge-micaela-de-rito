using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

public class SpoolWorkflowServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 23, 12, 0, 0, TimeSpan.Zero);

    private SpoolWorkflowService Build(IEnumerable<Spool>? spools = null)
        => new(new FakeSpoolRepository(spools), new FakeClock(_now));

    [Fact]
    public async Task AdvanceAsync_returns_NotFound_when_spool_missing()
    {
        var svc = Build();
        var result = await svc.AdvanceAsync("nonexistent");
        Assert.Equal(TransitionResult.NotFound, result);
    }

    [Fact]
    public async Task AdvanceAsync_returns_InvalidTransition_when_spool_at_Installed()
    {
        var spool = SpoolFactory.Create(history:
        [
            new StatusEvent(Station.Installed, _now.AddDays(-1), "system")
        ]);
        var svc = Build([spool]);

        var result = await svc.AdvanceAsync(spool.Id);

        Assert.Equal(TransitionResult.InvalidTransition, result);
    }

    [Fact]
    public async Task AdvanceAsync_returns_Success_and_advances_station()
    {
        var spool = SpoolFactory.Create(); // no history → Detailing
        var repo = new FakeSpoolRepository([spool]);
        var svc = new SpoolWorkflowService(repo, new FakeClock(_now));

        var result = await svc.AdvanceAsync(spool.Id);

        Assert.Equal(TransitionResult.Success, result);
        var updated = await repo.GetByIdAsync(spool.Id);
        Assert.Equal(Station.Cut, updated!.CurrentStation);
    }

    [Fact]
    public async Task AdvanceAsync_records_system_as_changed_by()
    {
        var spool = SpoolFactory.Create();
        var repo = new FakeSpoolRepository([spool]);
        var svc = new SpoolWorkflowService(repo, new FakeClock(_now));

        await svc.AdvanceAsync(spool.Id);

        var updated = await repo.GetByIdAsync(spool.Id);
        Assert.Equal("system", updated!.StatusHistory.Last().ChangedBy);
    }

    [Fact]
    public async Task AdvanceAsync_uses_clock_timestamp()
    {
        var spool = SpoolFactory.Create();
        var repo = new FakeSpoolRepository([spool]);
        var svc = new SpoolWorkflowService(repo, new FakeClock(_now));

        await svc.AdvanceAsync(spool.Id);

        var updated = await repo.GetByIdAsync(spool.Id);
        Assert.Equal(_now, updated!.StatusHistory.Last().ChangedAt);
    }

    [Theory]
    [InlineData(Station.Detailing, Station.Cut)]
    [InlineData(Station.Cut, Station.Weld)]
    [InlineData(Station.Weld, Station.QC)]
    [InlineData(Station.QC, Station.Shipped)]
    [InlineData(Station.Shipped, Station.Installed)]
    public async Task AdvanceAsync_advances_each_station_correctly(Station current, Station expected)
    {
        var spool = SpoolFactory.Create(history:
        [
            new StatusEvent(current, _now.AddDays(-1), "system")
        ]);
        var repo = new FakeSpoolRepository([spool]);
        var svc = new SpoolWorkflowService(repo, new FakeClock(_now));

        var result = await svc.AdvanceAsync(spool.Id);

        Assert.Equal(TransitionResult.Success, result);
        var updated = await repo.GetByIdAsync(spool.Id);
        Assert.Equal(expected, updated!.CurrentStation);
    }
}
