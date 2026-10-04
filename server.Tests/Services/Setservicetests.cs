using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Repositories;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services;

public class SetServiceTests
{
    private class FakeSetRepository : ISetRepository
    {
        public readonly List<VocabularySet> Sets = [];
        public readonly List<VocabularyCard> Cards = [];
        private int _nextSetId = 1;
        private int _nextCardId = 1;

        public Task<List<SetListItem>> GetByUserAsync(string userId) =>
            Task.FromResult(Sets
                .Where(s => s.UserId == userId)
                .Select(s => new SetListItem(s.Id, s.Name, Cards.Count(c => c.SetId == s.Id), s.CreatedAt))
                .ToList());

        public Task<VocabularySet?> GetOwnedWithCardsAsync(string userId, int setId)
        {
            var set = Sets.FirstOrDefault(s => s.UserId == userId && s.Id == setId);
            if (set is not null)
                set.Cards = Cards.Where(c => c.SetId == setId).ToList();
            return Task.FromResult(set);
        }

        public Task<bool> ExistsForUserAsync(string userId, int setId) =>
            Task.FromResult(Sets.Any(s => s.UserId == userId && s.Id == setId));

        public Task<List<VocabularyCard>> GetCardsAsync(int setId) =>
            Task.FromResult(Cards.Where(c => c.SetId == setId).ToList());

        private static void AssignId(object entity, int id) =>
            entity.GetType().GetProperty("Id")!.GetSetMethod(nonPublic: true)!.Invoke(entity, [id]);

        public Task<VocabularySet> AddAsync(VocabularySet set)
        {
            AssignId(set, _nextSetId++);
            Sets.Add(set);
            return Task.FromResult(set);
        }

        public Task<VocabularyCard> AddCardAsync(VocabularyCard card)
        {
            AssignId(card, _nextCardId++);
            Cards.Add(card);
            return Task.FromResult(card);
        }
    }

    private readonly FakeSetRepository _repo = new();
    private readonly SetService _service;

    public SetServiceTests()
    {
        _service = new SetService(_repo);
    }

    [Fact]
    public async Task Create_SavesSetForUser_AndReturnsSummaryWithZeroCards()
    {
        var summary = await _service.CreateAsync("user-1", "Animals");

        Assert.Equal("Animals", summary.Name);
        Assert.Equal(0, summary.CardCount);
        var saved = Assert.Single(_repo.Sets);
        Assert.Equal("user-1", saved.UserId);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyTheUsersOwnSets_WithCardCounts()
    {
        var mine = await _service.CreateAsync("user-1", "Mine");
        await _service.CreateAsync("user-2", "Theirs");
        await _service.AddCardAsync("user-1", mine.Id, "cat", "kat");
        await _service.AddCardAsync("user-1", mine.Id, "dog", "šuo");

        var result = await _service.GetAllAsync("user-1");

        var only = Assert.Single(result);
        Assert.Equal("Mine", only.Name);
        Assert.Equal(2, only.CardCount);
    }

    [Fact]
    public async Task Get_OwnSet_ReturnsItsCards()
    {
        var set = await _service.CreateAsync("user-1", "Mine");
        await _service.AddCardAsync("user-1", set.Id, "cat", "kat");

        var contents = await _service.GetAsync("user-1", set.Id);

        Assert.Equal("Mine", contents.Name);
        var card = Assert.Single(contents.Cards);
        Assert.Equal("cat", card.Term);
    }

    [Fact]
    public async Task Get_SetOwnedBySomeoneElse_ThrowsNotFound()
    {
        var set = await _service.CreateAsync("user-1", "Mine");

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync("user-2", set.Id));
    }

    [Fact]
    public async Task Get_MissingSet_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync("user-1", 999));
    }

    [Fact]
    public async Task AddCard_ToOwnSet_SavesCardAndReturnsIt()
    {
        var set = await _service.CreateAsync("user-1", "Mine");

        var card = await _service.AddCardAsync("user-1", set.Id, "cat", "kat");

        Assert.Equal("cat", card.Term);
        Assert.Equal("kat", card.Definition);
        Assert.Single(_repo.Cards);
    }

    [Fact]
    public async Task AddCard_ToSomeoneElsesSet_ThrowsNotFound_AndSavesNothing()
    {
        var set = await _service.CreateAsync("user-1", "Mine");

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AddCardAsync("user-2", set.Id, "cat", "kat"));

        Assert.Empty(_repo.Cards);
    }

    [Fact]
    public async Task GetCards_SomeoneElsesSet_ThrowsNotFound()
    {
        var set = await _service.CreateAsync("user-1", "Mine");

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetCardsAsync("user-2", set.Id));
    }
}