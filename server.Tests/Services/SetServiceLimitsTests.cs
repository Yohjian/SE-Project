using NSubstitute;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Helpers;
using QuattroLingo.Interfaces.Repositories;
using QuattroLingo.Repositories;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services
{
    public class SetServiceLimitsTests
    {
        private readonly ISetRepository _repo = Substitute.For<ISetRepository>();
        private readonly SetService _service;

        public SetServiceLimitsTests()
        {
            var db = TestDb.Create();
            db.SetFolders.Add(new SetFolder { Id = 1, UserId = "user-1", Name = "Folder" });
            db.SaveChanges();

            _service = new SetService(_repo, db);
            _repo.ExistsForUserAsync("user-1", 1).Returns(true);
        }

        private static string Long(int n) => new('x', n);

        private void ExistingCards(int count) =>
            _repo.GetCardsAsync(1).Returns(
                Enumerable.Range(0, count)
                    .Select(i => new VocabularyCard { SetId = 1, Term = $"t{i}", Definition = "d" })
                    .ToList());

        [Fact]
        public async Task CreateSet_NameTooLong_ThrowsValidation() =>
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.CreateAsync("user-1", Long(Limits.SetNameMax + 1), 1));

        [Fact]
        public async Task CreateSet_UserAtSetLimit_ThrowsValidationAndSavesNothing()
        {
            _repo.GetByUserAsync("user-1").Returns(
                Enumerable.Range(0, Limits.SetsPerUser)
                    .Select(i => new SetListItem(i + 1, $"s{i}", 0, DateTime.UtcNow))
                    .ToList());

            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync("user-1", "New", 1));

            await _repo.DidNotReceive().AddAsync(Arg.Any<VocabularySet>());
        }

        [Fact]
        public async Task AddCard_SetAtCardLimit_ThrowsValidation()
        {
            ExistingCards(Limits.CardsPerSet);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddCardAsync("user-1", 1, "new", "def"));

            await _repo.DidNotReceive().AddCardAsync(Arg.Any<VocabularyCard>());
        }

        [Fact]
        public async Task AddCard_TermTooLong_ThrowsValidation()
        {
            ExistingCards(0);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddCardAsync("user-1", 1, Long(Limits.CardTextMax + 1), "def"));
        }

        [Fact]
        public async Task AddCard_DefinitionTooLong_ThrowsValidation()
        {
            ExistingCards(0);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddCardAsync("user-1", 1, "term", Long(Limits.CardTextMax + 1)));
        }
    }
}
