using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

public class SpoolTests
{
    [Fact]
    public void CurrentStation_is_Detailing_when_no_history()
    {
        var spool = SpoolFactory.Create();
        Assert.Equal(Station.Detailing, spool.CurrentStation);
    }

    [Fact]
    public void CurrentStation_returns_latest_status_event_station()
    {
        var now = DateTimeOffset.UtcNow;
        var spool = SpoolFactory.Create(history:
        [
            new StatusEvent(Station.Cut, now.AddHours(-2), "system"),
            new StatusEvent(Station.Weld, now, "system"),
        ]);

        Assert.Equal(Station.Weld, spool.CurrentStation);
    }

    [Fact]
    public void CurrentStation_picks_most_recent_event_regardless_of_list_order()
    {
        var now = DateTimeOffset.UtcNow;
        var spool = SpoolFactory.Create(history:
        [
            new StatusEvent(Station.QC, now, "system"),
            new StatusEvent(Station.Weld, now.AddHours(-1), "system"),
        ]);

        Assert.Equal(Station.QC, spool.CurrentStation);
    }
}
