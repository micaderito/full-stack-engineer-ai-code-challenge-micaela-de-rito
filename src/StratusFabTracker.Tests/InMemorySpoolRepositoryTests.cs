using StratusFabTracker.Api.Domain;
using StratusFabTracker.Api.Infrastructure;
using Xunit;

namespace StratusFabTracker.Tests;

public class InMemorySpoolRepositoryTests
{
    private static InMemorySpoolRepository Seeded(params Spool[] spools)
    {
        var repo = new InMemorySpoolRepository();
        repo.SeedAsync(spools).GetAwaiter().GetResult();
        return repo;
    }

    [Fact]
    public async Task GetAllAsync_returns_empty_list_when_no_spools()
    {
        var repo = new InMemorySpoolRepository();
        var result = await repo.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task SeedAsync_stores_spools_retrievable_by_GetAll()
    {
        var s1 = SpoolFactory.Create("S1");
        var s2 = SpoolFactory.Create("S2");
        var repo = Seeded(s1, s2);

        var result = await repo.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Id == "S1");
        Assert.Contains(result, s => s.Id == "S2");
    }

    [Fact]
    public async Task SeedAsync_overwrites_existing_spool_with_same_id()
    {
        var original = SpoolFactory.Create("S1", spoolNumber: "001");
        var repo = Seeded(original);

        var replacement = SpoolFactory.Create("S1", spoolNumber: "999");
        await repo.SeedAsync([replacement]);

        var all = await repo.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("999", all[0].SpoolNumber);
    }

    [Fact]
    public async Task GetByIdAsync_returns_spool_when_found()
    {
        var spool = SpoolFactory.Create("S42");
        var repo = Seeded(spool);

        var result = await repo.GetByIdAsync("S42");

        Assert.NotNull(result);
        Assert.Equal("S42", result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        var repo = new InMemorySpoolRepository();
        var result = await repo.GetByIdAsync("missing");
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_replaces_spool_in_store()
    {
        var now = DateTimeOffset.UtcNow;
        var spool = SpoolFactory.Create("S1");
        var repo = Seeded(spool);

        spool.StatusHistory.Add(new StatusEvent(Station.Cut, now, "system"));
        await repo.UpdateAsync(spool);

        var updated = await repo.GetByIdAsync("S1");
        Assert.NotNull(updated);
        Assert.Equal(Station.Cut, updated.CurrentStation);
    }

    [Fact]
    public async Task UpdateAsync_upserts_spool_not_previously_stored()
    {
        var repo = new InMemorySpoolRepository();
        var spool = SpoolFactory.Create("S99");

        await repo.UpdateAsync(spool);

        var result = await repo.GetByIdAsync("S99");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAllAsync_returns_independent_copy_each_call()
    {
        var repo = Seeded(SpoolFactory.Create("S1"), SpoolFactory.Create("S2"));
        var first  = await repo.GetAllAsync();
        var second = await repo.GetAllAsync();
        Assert.NotSame(first, second);
    }
}
