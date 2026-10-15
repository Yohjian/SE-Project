using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Repositories;

namespace QuattroLingo.Tests.Repositories;

public class SetRepositoryTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedAsync(AppDbContext db)
    {
        db.Sets.Add(new VocabularySet
        {
            UserId = "user-1",
            Name = "Mine",
            CreatedAt = DateTime.UtcNow,
            Cards =
            [
                new VocabularyCard { Term = "cat", Definition = "kat" },
                new VocabularyCard { Term = "dog", Definition = "šuo" }
            ]
        });
        db.Sets.Add(new VocabularySet
        {
            UserId = "user-2",
            Name = "Theirs",
            CreatedAt = DateTime.UtcNow,
            Cards = []
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUser_ReturnsOnlyThatUsersSets_WithCardCounts()
    {
        await using var db = CreateDb();
        await SeedAsync(db);

        var result = await new SetRepository(db).GetByUserAsync("user-1");

        var item = Assert.Single(result);
        Assert.Equal("Mine", item.Name);
        Assert.Equal(2, item.CardCount);
    }

    [Fact]
    public async Task GetOwnedWithCards_OtherUsersSet_ReturnsNull()
    {
        await using var db = CreateDb();
        await SeedAsync(db);
        var mineId = db.Sets.Single(s => s.UserId == "user-1").Id;

        var result = await new SetRepository(db).GetOwnedWithCardsAsync("user-2", mineId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOwnedWithCards_OwnSet_IncludesCards()
    {
        await using var db = CreateDb();
        await SeedAsync(db);
        var mineId = db.Sets.Single(s => s.UserId == "user-1").Id;

        var result = await new SetRepository(db).GetOwnedWithCardsAsync("user-1", mineId);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Cards.Count);
    }

    [Fact]
    public async Task ExistsForUser_IsTrueOnlyForTheOwner()
    {
        await using var db = CreateDb();
        await SeedAsync(db);
        var repo = new SetRepository(db);
        var mineId = db.Sets.Single(s => s.UserId == "user-1").Id;

        Assert.True(await repo.ExistsForUserAsync("user-1", mineId));
        Assert.False(await repo.ExistsForUserAsync("user-2", mineId));
        Assert.False(await repo.ExistsForUserAsync("user-1", 999));
    }
}