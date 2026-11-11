using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Repositories;

namespace QuattroLingo.Tests.Repositories
{
    public class SetRepositoryTests : IAsyncLifetime
    {
        private readonly AppDbContext _db = TestDb.Create();
        private readonly SetRepository _repository;
        private int _mineId;
        private int _theirsId;

        public SetRepositoryTests()
        {
            _repository = new SetRepository(_db);
        }

        /// <summary>Seeds "Mine" (user-1, two cards) and "Theirs" (user-2, no cards).</summary>
        public async Task InitializeAsync()
        {
            var mine = new VocabularySet
            {
                UserId = "user-1",
                Name = "Mine",
                CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                Cards =
                [
                    new VocabularyCard { Term = "cat", Definition = "katė" },
                    new VocabularyCard { Term = "dog", Definition = "šuo" }
                ]
            };
            var theirs = new VocabularySet { UserId = "user-2", Name = "Theirs", CreatedAt = DateTime.UtcNow };

            _db.Sets.AddRange(mine, theirs);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();   // make the repository load from the store, not the tracker

            _mineId = mine.Id;
            _theirsId = theirs.Id;
        }

        public async Task DisposeAsync() => await _db.DisposeAsync();



        [Fact]
        public async Task GetByUserAsync_SeveralUsers_ReturnsOnlyThatUsersSetsWithCardCounts()
        {
            var item = Assert.Single(await _repository.GetByUserAsync("user-1"));

            Assert.Equal(_mineId, item.Id);
            Assert.Equal("Mine", item.Name);
            Assert.Equal(2, item.CardCount);
            Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), item.CreatedAt);
        }

        [Fact]
        public async Task GetByUserAsync_SetWithoutCards_ReturnsZeroCardCount()
        {
            Assert.Equal(0, Assert.Single(await _repository.GetByUserAsync("user-2")).CardCount);
        }

        [Fact]
        public async Task GetByUserAsync_UserWithoutSets_ReturnsEmptyList()
        {
            Assert.Empty(await _repository.GetByUserAsync("user-3"));
        }



        [Fact]
        public async Task GetOwnedWithCardsAsync_OwnSet_ReturnsSetWithCards()
        {
            var set = await _repository.GetOwnedWithCardsAsync("user-1", _mineId);

            Assert.NotNull(set);
            Assert.Equal(["cat", "dog"], set.Cards.Select(c => c.Term).Order());
        }

        [Fact]
        public async Task GetOwnedWithCardsAsync_OtherUsersSet_ReturnsNull()
        {
            Assert.Null(await _repository.GetOwnedWithCardsAsync("user-2", _mineId));
        }

        [Fact]
        public async Task GetOwnedWithCardsAsync_UnknownSet_ReturnsNull()
        {
            Assert.Null(await _repository.GetOwnedWithCardsAsync("user-1", 999));
        }



        [Fact]
        public async Task ExistsForUserAsync_Owner_ReturnsTrue()
        {
            Assert.True(await _repository.ExistsForUserAsync("user-1", _mineId));
        }

        [Fact]
        public async Task ExistsForUserAsync_OtherUser_ReturnsFalse()
        {
            Assert.False(await _repository.ExistsForUserAsync("user-2", _mineId));
        }

        [Fact]
        public async Task ExistsForUserAsync_UnknownSet_ReturnsFalse()
        {
            Assert.False(await _repository.ExistsForUserAsync("user-1", 999));
        }



        [Fact]
        public async Task GetCardsAsync_SetWithCards_ReturnsOnlyThatSetsCards()
        {
            _db.Cards.Add(new VocabularyCard { Term = "bird", Definition = "paukštis", SetId = _theirsId });
            await _db.SaveChangesAsync();

            var cards = await _repository.GetCardsAsync(_mineId);

            Assert.Equal(["cat", "dog"], cards.Select(c => c.Term).Order());
        }

        [Fact]
        public async Task GetCardsAsync_SetWithoutCards_ReturnsEmptyList()
        {
            Assert.Empty(await _repository.GetCardsAsync(_theirsId));
        }



        [Fact]
        public async Task AddAsync_NewSet_SavesItAndAssignsId()
        {
            var set = await _repository.AddAsync(new VocabularySet { UserId = "user-3", Name = "New", CreatedAt = DateTime.UtcNow });

            Assert.NotEqual(0, set.Id);
            Assert.True(await _db.Sets.AnyAsync(s => s.Id == set.Id && s.UserId == "user-3"));
        }



        [Fact]
        public async Task AddCardAsync_ExistingSet_SavesCardUnderSetAndAssignsId()
        {
            var card = await _repository.AddCardAsync(new VocabularyCard { Term = "fish", Definition = "žuvis", SetId = _mineId });

            Assert.NotEqual(0, card.Id);
            Assert.Equal(3, await _db.Cards.CountAsync(c => c.SetId == _mineId));
        }



        [Fact]
        public async Task DeleteSetAsync_ExistingSet_RemovesOnlyThatSet()
        {
            await _repository.DeleteSetAsync(_mineId);

            // Cards are removed by the database's ON DELETE CASCADE (see AppDbContext), which the
            // EF in-memory provider does not emulate for untracked rows, so only the set is checked here.
            Assert.False(await _db.Sets.AnyAsync(s => s.Id == _mineId));
            Assert.True(await _db.Sets.AnyAsync(s => s.Id == _theirsId));
        }

        [Fact]
        public async Task DeleteSetAsync_UnknownSet_DoesNothing()
        {
            await _repository.DeleteSetAsync(999);

            Assert.Equal(2, await _db.Sets.CountAsync());
            Assert.Equal(2, await _db.Cards.CountAsync());
        }



        [Fact]
        public async Task DeleteCardAsync_ExistingCard_RemovesOnlyThatCard()
        {
            var catId = (await _db.Cards.SingleAsync(c => c.Term == "cat")).Id;

            await _repository.DeleteCardAsync(catId);

            Assert.Equal("dog", (await _db.Cards.SingleAsync()).Term);
        }

        [Fact]
        public async Task DeleteCardAsync_UnknownCard_DoesNothing()
        {
            await _repository.DeleteCardAsync(999);

            Assert.Equal(2, await _db.Cards.CountAsync());
        }
    }
}
