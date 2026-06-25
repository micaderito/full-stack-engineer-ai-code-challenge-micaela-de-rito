using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

public class StationTests
{
    [Fact]
    public void Detailing_advances_to_Cut()
    {
        Assert.Equal(Station.Cut, Station.Detailing.Next());
    }

    [Theory]
    [InlineData(Station.Detailing, Station.Cut)]
    [InlineData(Station.Cut,       Station.Weld)]
    [InlineData(Station.Weld,      Station.QC)]
    [InlineData(Station.QC,        Station.Shipped)]
    [InlineData(Station.Shipped,   Station.Installed)]
    public void Each_station_advances_to_correct_next(Station current, Station expected)
    {
        Assert.Equal(expected, current.Next());
    }

    [Fact]
    public void Installed_has_no_next_station()
    {
        Assert.Null(Station.Installed.Next());
    }
}
