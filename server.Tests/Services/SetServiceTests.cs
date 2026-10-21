using System.Text.Json;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Repositories;
using QuattroLingo.Services;


namespace QuattroLingo.Tests.Services
{
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

            public Task DeleteSetAsync(int setId)
            {
                Sets.RemoveAll(s => s.Id == setId);
                Cards.RemoveAll(c => c.SetId == setId);
                return Task.CompletedTask;
            }

            public Task DeleteCardAsync(int cardId)
            {
                Cards.RemoveAll(c => c.Id == cardId);
                return Task.CompletedTask;
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

        // ---------- Export ----------

        private static async Task<string> ReadAllAsync(Stream stream)
        {
            using var reader = new StreamReader(stream); // also strips the UTF-8 BOM
            return await reader.ReadToEndAsync();
        }

        [Fact]
        public async Task DownloadJson_OwnSet_ReturnsNameAndAllCards()
        {
            var set = await _service.CreateAsync("user-1", "Animals");
            await _service.AddCardAsync("user-1", set.Id, "cat", "kat");
            await _service.AddCardAsync("user-1", set.Id, "dog", "šuo");

            var (stream, mime, filename) = await _service.DownloadSetAsync("user-1", set.Id, FileType.Json);

            Assert.Equal("application/json", mime);
            Assert.Equal("Animals.json", filename);

            using var doc = JsonDocument.Parse(await ReadAllAsync(stream));
            var root = doc.RootElement;
            Assert.Equal("Animals", root.GetProperty("name").GetString());

            var cards = root.GetProperty("cards").EnumerateArray().ToList();
            Assert.Equal(2, cards.Count);
            Assert.Equal("cat", cards[0].GetProperty("term").GetString());
            Assert.Equal("kat", cards[0].GetProperty("definition").GetString());
            Assert.Equal("šuo", cards[1].GetProperty("definition").GetString());
        }

        [Fact]
        public async Task DownloadCsv_OwnSet_WritesHeaderAndOneRowPerCard()
        {
            var set = await _service.CreateAsync("user-1", "Animals");
            await _service.AddCardAsync("user-1", set.Id, "cat", "kat");
            await _service.AddCardAsync("user-1", set.Id, "dog", "šuo");

            var (stream, mime, filename) = await _service.DownloadSetAsync("user-1", set.Id, FileType.Csv);

            Assert.Equal("text/csv", mime);
            Assert.Equal("Animals.csv", filename);

            var lines = (await ReadAllAsync(stream))
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(["Term,Definition", "\"cat\",\"kat\"", "\"dog\",\"šuo\""], lines);
        }

        [Fact]
        public async Task DownloadCsv_EscapesQuotesCommasAndNewlines()
        {
            var set = await _service.CreateAsync("user-1", "Tricky");
            await _service.AddCardAsync("user-1", set.Id, "say \"hi\", ok", "line one\nline two");

            var (stream, _, _) = await _service.DownloadSetAsync("user-1", set.Id, FileType.Csv);
            var csv = await ReadAllAsync(stream);

            // Quotes are doubled; the comma and newline stay inside the quoted field.
            Assert.Contains("\"say \"\"hi\"\", ok\",\"line one\nline two\"", csv);
        }

        [Fact]
        public async Task Download_SetOwnedBySomeoneElse_ThrowsNotFound()
        {
            var set = await _service.CreateAsync("user-1", "Mine");

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.DownloadSetAsync("user-2", set.Id, FileType.Json));
        }

        [Fact]
        public async Task Download_MissingSet_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.DownloadSetAsync("user-1", 999, FileType.Csv));
        }

        [Fact]
        public async Task Download_UnknownFileType_ThrowsUnsupportedFileType()
        {
            var set = await _service.CreateAsync("user-1", "Mine");

            await Assert.ThrowsAsync<UnsupportedFileTypeException>(
                () => _service.DownloadSetAsync("user-1", set.Id, (FileType)99));

        }

        // ---------- Delete ----------

        [Fact]
        public async Task DeleteSet_OwnSet_RemovesSetAndItsCards()
        {
            var set = await _service.CreateAsync("user-1", "Mine");
            await _service.AddCardAsync("user-1", set.Id, "cat", "kat");

            await _service.DeleteSetAsync("user-1", set.Id);

            Assert.Empty(_repo.Sets);
            Assert.Empty(_repo.Cards);
        }

        [Fact]
        public async Task DeleteSet_SomeoneElsesSet_ThrowsNotFound_AndKeepsIt()
        {
            var set = await _service.CreateAsync("user-1", "Mine");

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteSetAsync("user-2", set.Id));

            Assert.Single(_repo.Sets);
        }

        [Fact]
        public async Task DeleteCard_OwnCard_RemovesOnlyThatCard()
        {
            var set = await _service.CreateAsync("user-1", "Mine");
            var cat = await _service.AddCardAsync("user-1", set.Id, "cat", "kat");
            await _service.AddCardAsync("user-1", set.Id, "dog", "šuo");

            await _service.DeleteCardAsync("user-1", set.Id, cat.Id);

            var left = Assert.Single(_repo.Cards);
            Assert.Equal("dog", left.Term);
        }

        [Fact]
        public async Task DeleteCard_InSomeoneElsesSet_ThrowsNotFound()
        {
            var set = await _service.CreateAsync("user-1", "Mine");
            var card = await _service.AddCardAsync("user-1", set.Id, "cat", "kat");

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.DeleteCardAsync("user-2", set.Id, card.Id));

            Assert.Single(_repo.Cards);
        }

        [Fact]
        public async Task DeleteCard_FromADifferentSet_ThrowsNotFound()
        {
            var mine = await _service.CreateAsync("user-1", "Mine");
            var other = await _service.CreateAsync("user-1", "Other");
            var card = await _service.AddCardAsync("user-1", other.Id, "cat", "kat");

            // Card exists, but not in the set given in the URL
            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.DeleteCardAsync("user-1", mine.Id, card.Id));

            Assert.Single(_repo.Cards);
        }

        // ---------- Duplicate terms ----------

        [Fact]
        public async Task AddCard_DuplicateTerm_IgnoringCase_ThrowsValidation()
        {
            var set = await _service.CreateAsync("user-1", "Mine");
            await _service.AddCardAsync("user-1", set.Id, "cat", "kat");

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddCardAsync("user-1", set.Id, "Cat", "katė"));

            Assert.Single(_repo.Cards);
        }

        [Fact]
        public async Task AddCard_SameTermInDifferentSets_IsAllowed()
        {
            var a = await _service.CreateAsync("user-1", "A");
            var b = await _service.CreateAsync("user-1", "B");

            await _service.AddCardAsync("user-1", a.Id, "cat", "kat");
            await _service.AddCardAsync("user-1", b.Id, "cat", "kat");

            Assert.Equal(2, _repo.Cards.Count);
        }
    }
}
