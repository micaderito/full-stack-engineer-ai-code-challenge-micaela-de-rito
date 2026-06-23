using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Tests;

internal sealed class FakeSpoolRepository : ISpoolRepository
{
    private readonly List<Spool> _spools;

    public FakeSpoolRepository(IEnumerable<Spool>? spools = null)
        => _spools = spools?.ToList() ?? [];

    public Task<List<Spool>> GetAllAsync() => Task.FromResult(_spools.ToList());

    public Task<Spool?> GetByIdAsync(string id)
        => Task.FromResult(_spools.FirstOrDefault(s => s.Id == id));

    public Task UpdateAsync(Spool spool)
    {
        var idx = _spools.FindIndex(s => s.Id == spool.Id);
        if (idx >= 0) _spools[idx] = spool;
        return Task.CompletedTask;
    }

    public Task SeedAsync(IEnumerable<Spool> spools)
    {
        _spools.AddRange(spools);
        return Task.CompletedTask;
    }
}

internal sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow) => UtcNow = utcNow;
    public DateTimeOffset UtcNow { get; }
}

internal static class SpoolFactory
{
    public static Spool Create(
        string id = "S1",
        string packageId = "PKG-1",
        string spoolNumber = "001",
        DateOnly? dueDate = null,
        List<StatusEvent>? history = null) => new()
    {
        Id = id,
        PackageId = packageId,
        SpoolNumber = spoolNumber,
        DueDate = dueDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
        Bom = [],
        StatusHistory = history ?? []
    };
}
