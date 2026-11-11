using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Repositories;

namespace QuattroLingo.Tests.Repositories
{
    public class QuizRepositoryTests : IAsyncLifetime
    {
        private readonly AppDbContext _db = TestDb.Create();
        private readonly QuizRepository _repository;

        public QuizRepositoryTests()
        {
            _repository = new QuizRepository(_db);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync() => await _db.DisposeAsync();

        private static Quiz MakeQuiz(string ownerId = "user-1", string title = "Lithuanian basics",
            bool isPublic = false, int? sourceQuizId = null) => new()
        {
            OwnerId = ownerId,
            Title = title,
            IsPublic = isPublic,
            SourceQuizId = sourceQuizId,
            Questions =
            [
                new Question
                {
                    Text = "Labas means?",
                    TimeLimitSeconds = 20,
                    Points = 1000,
                    AnswerOptions =
                    [
                        new AnswerOption { Text = "Hello", IsCorrect = true },
                        new AnswerOption { Text = "Bye", IsCorrect = false }
                    ]
                }
            ]
        };

        private async Task<Quiz> SeedAsync(Quiz quiz)
        {
            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();   // make the repository load from the store, not the tracker
            return quiz;
        }

        private async Task SeedUsersAsync(params (string Id, string? DisplayName)[] users)
        {
            _db.Users.AddRange(users.Select(u => new ApplicationUser { Id = u.Id, DisplayName = u.DisplayName }));
            await _db.SaveChangesAsync();
        }

        // ---- GetByOwnerIdAsync ----

        [Fact]
        public async Task GetByOwnerIdAsync_SeveralOwners_ReturnsOnlyThatOwnersQuizzes()
        {
            await SeedAsync(MakeQuiz("user-1", "Mine"));
            await SeedAsync(MakeQuiz("user-2", "Theirs"));

            var result = await _repository.GetByOwnerIdAsync("user-1");

            Assert.Equal("Mine", Assert.Single(result).Title);
        }

        [Fact]
        public async Task GetByOwnerIdAsync_OwnQuiz_IncludesQuestionsAndAnswerOptions()
        {
            await SeedAsync(MakeQuiz());

            var quiz = Assert.Single(await _repository.GetByOwnerIdAsync("user-1"));

            var question = Assert.Single(quiz.Questions);
            Assert.Equal(2, question.AnswerOptions.Count);
        }

        [Fact]
        public async Task GetByOwnerIdAsync_OwnerWithoutQuizzes_ReturnsEmptyList()
        {
            await SeedAsync(MakeQuiz("user-2"));

            Assert.Empty(await _repository.GetByOwnerIdAsync("user-1"));
        }

        // ---- GetByIdAsync ----

        [Fact]
        public async Task GetByIdAsync_ExistingQuiz_IncludesQuestionsAndAnswerOptions()
        {
            var seeded = await SeedAsync(MakeQuiz());

            var quiz = await _repository.GetByIdAsync(seeded.Id);

            Assert.NotNull(quiz);
            Assert.Equal(2, Assert.Single(quiz.Questions).AnswerOptions.Count);
        }

        [Fact]
        public async Task GetByIdAsync_UnknownId_ReturnsNull()
        {
            Assert.Null(await _repository.GetByIdAsync(999));
        }

        // ---- GetCopiesBySourceAsync ----

        [Fact]
        public async Task GetCopiesBySourceAsync_SeveralCopies_ReturnsOnlyThatUsersCopiesOfThatSourceOrderedById()
        {
            var source = await SeedAsync(MakeQuiz("author", isPublic: true));
            var first = await SeedAsync(MakeQuiz("user-1", "Copy 1", sourceQuizId: source.Id));
            var second = await SeedAsync(MakeQuiz("user-1", "Copy 2", sourceQuizId: source.Id));
            await SeedAsync(MakeQuiz("user-2", "Someone else's copy", sourceQuizId: source.Id));
            await SeedAsync(MakeQuiz("user-1", "Unrelated quiz"));

            var copies = await _repository.GetCopiesBySourceAsync("user-1", source.Id);

            Assert.Equal(new[] { first.Id, second.Id }, copies.Select(q => q.Id));
        }

        [Fact]
        public async Task GetCopiesBySourceAsync_NoCopies_ReturnsEmptyList()
        {
            var source = await SeedAsync(MakeQuiz("author", isPublic: true));

            Assert.Empty(await _repository.GetCopiesBySourceAsync("user-1", source.Id));
        }

        // ---- AddAsync / UpdateAsync / DeleteAsync ----

        [Fact]
        public async Task AddAsync_NewQuiz_SavesQuizWithQuestionsAndOptions()
        {
            await _repository.AddAsync(MakeQuiz());

            Assert.Equal(1, await _db.Quizzes.CountAsync());
            Assert.Equal(1, await _db.Questions.CountAsync());
            Assert.Equal(2, await _db.AnswerOptions.CountAsync());
        }

        [Fact]
        public async Task UpdateAsync_ChangedTitle_PersistsChange()
        {
            var seeded = await SeedAsync(MakeQuiz(title: "Old"));
            var quiz = (await _repository.GetByIdAsync(seeded.Id))!;

            quiz.Title = "New";
            await _repository.UpdateAsync(quiz);
            _db.ChangeTracker.Clear();

            Assert.Equal("New", (await _db.Quizzes.SingleAsync()).Title);
        }

        [Fact]
        public async Task DeleteAsync_LoadedQuiz_RemovesQuizQuestionsAndOptions()
        {
            var seeded = await SeedAsync(MakeQuiz());
            var quiz = (await _repository.GetByIdAsync(seeded.Id))!;

            await _repository.DeleteAsync(quiz);

            Assert.Empty(await _db.Quizzes.ToListAsync());
            Assert.Empty(await _db.Questions.ToListAsync());
            Assert.Empty(await _db.AnswerOptions.ToListAsync());
        }

        // ---- GetQuestionByIdAsync ----

        [Fact]
        public async Task GetQuestionByIdAsync_ExistingQuestion_IncludesQuizAndAnswerOptions()
        {
            var seeded = await SeedAsync(MakeQuiz(title: "Parent"));
            var questionId = seeded.Questions.Single().Id;

            var question = await _repository.GetQuestionByIdAsync(questionId);

            Assert.NotNull(question);
            Assert.Equal("Parent", question.Quiz!.Title);
            Assert.Equal(2, question.AnswerOptions.Count);
        }

        [Fact]
        public async Task GetQuestionByIdAsync_UnknownId_ReturnsNull()
        {
            Assert.Null(await _repository.GetQuestionByIdAsync(999));
        }

        // ---- GetAnswerOptionByIdAsync ----

        [Fact]
        public async Task GetAnswerOptionByIdAsync_ExistingOption_IncludesQuestionItsQuizAndSiblingOptions()
        {
            var seeded = await SeedAsync(MakeQuiz(title: "Parent"));
            var optionId = seeded.Questions.Single().AnswerOptions.First().Id;

            var option = await _repository.GetAnswerOptionByIdAsync(optionId);

            Assert.NotNull(option);
            Assert.Equal("Labas means?", option.Question!.Text);
            Assert.Equal("Parent", option.Question.Quiz!.Title);
            Assert.Equal(2, option.Question.AnswerOptions.Count);
        }

        [Fact]
        public async Task GetAnswerOptionByIdAsync_UnknownId_ReturnsNull()
        {
            Assert.Null(await _repository.GetAnswerOptionByIdAsync(999));
        }

        // ---- AddQuestionAsync / DeleteQuestionAsync ----

        [Fact]
        public async Task AddQuestionAsync_ExistingQuiz_SavesQuestionUnderQuiz()
        {
            var seeded = await SeedAsync(MakeQuiz());

            await _repository.AddQuestionAsync(new Question { QuizId = seeded.Id, Text = "Ačiū means?" });

            Assert.Equal(2, await _db.Questions.CountAsync(q => q.QuizId == seeded.Id));
        }

        [Fact]
        public async Task DeleteQuestionAsync_LoadedQuestion_RemovesQuestionAndItsOptions()
        {
            var seeded = await SeedAsync(MakeQuiz());
            var question = (await _repository.GetQuestionByIdAsync(seeded.Questions.Single().Id))!;

            await _repository.DeleteQuestionAsync(question);

            Assert.Empty(await _db.Questions.ToListAsync());
            Assert.Empty(await _db.AnswerOptions.ToListAsync());
            Assert.Equal(1, await _db.Quizzes.CountAsync());
        }

        // ---- AddAnswerOptionAsync / DeleteAnswerOptionAsync ----

        [Fact]
        public async Task AddAnswerOptionAsync_ExistingQuestion_SavesOptionUnderQuestion()
        {
            var seeded = await SeedAsync(MakeQuiz());
            var questionId = seeded.Questions.Single().Id;

            await _repository.AddAnswerOptionAsync(new AnswerOption { QuestionId = questionId, Text = "Thanks" });

            Assert.Equal(3, await _db.AnswerOptions.CountAsync(a => a.QuestionId == questionId));
        }

        [Fact]
        public async Task DeleteAnswerOptionAsync_LoadedOption_RemovesOnlyThatOption()
        {
            var seeded = await SeedAsync(MakeQuiz());
            var option = (await _repository.GetAnswerOptionByIdAsync(
                seeded.Questions.Single().AnswerOptions.First(a => a.Text == "Bye").Id))!;

            await _repository.DeleteAnswerOptionAsync(option);

            Assert.Equal("Hello", (await _db.AnswerOptions.SingleAsync()).Text);
        }

        // ---- SaveChangesAsync ----

        [Fact]
        public async Task SaveChangesAsync_TrackedEntityModified_PersistsChange()
        {
            var seeded = await SeedAsync(MakeQuiz());
            var quiz = (await _repository.GetByIdAsync(seeded.Id))!;

            quiz.Description = "Edited";
            await _repository.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            Assert.Equal("Edited", (await _db.Quizzes.SingleAsync()).Description);
        }

        // ---- GetPublicAsync ----

        [Fact]
        public async Task GetPublicAsync_MixedVisibility_ReturnsOnlyPublicQuizzesOrderedByTitle()
        {
            await SeedUsersAsync(("user-1", "Ona"));
            await SeedAsync(MakeQuiz(title: "Zebra words", isPublic: true));
            await SeedAsync(MakeQuiz(title: "Animals", isPublic: true));
            await SeedAsync(MakeQuiz(title: "Private notes", isPublic: false));

            var result = await _repository.GetPublicAsync();

            Assert.Equal(new[] { "Animals", "Zebra words" }, result.Select(q => q.Title));
        }

        [Fact]
        public async Task GetPublicAsync_PublicQuiz_IncludesOwner()
        {
            await SeedUsersAsync(("user-1", "Ona"));
            await SeedAsync(MakeQuiz(isPublic: true));

            var quiz = Assert.Single(await _repository.GetPublicAsync());

            Assert.Equal("Ona", quiz.Owner!.DisplayName);
        }

        [Fact]
        public async Task GetPublicAsync_AnyQuiz_ReturnsUntrackedEntities()
        {
            await SeedUsersAsync(("user-1", "Ona"));
            await SeedAsync(MakeQuiz(isPublic: true));

            var quiz = Assert.Single(await _repository.GetPublicAsync());

            Assert.Equal(EntityState.Detached, _db.Entry(quiz).State);
        }

        // ---- HasActiveRoomAsync ----

        [Theory]
        [InlineData(RoomStatus.Waiting, true)]
        [InlineData(RoomStatus.InProgress, true)]
        [InlineData(RoomStatus.Finished, false)]
        [InlineData(RoomStatus.Cancelled, false)]
        public async Task HasActiveRoomAsync_RoomWithStatus_ReturnsTrueOnlyWhileWaitingOrInProgress(
            RoomStatus status, bool expected)
        {
            _db.Rooms.Add(new Room { QuizId = 1, HostId = "h", JoinCode = "111111", Status = status });
            await _db.SaveChangesAsync();

            Assert.Equal(expected, await _repository.HasActiveRoomAsync(1));
        }

        [Fact]
        public async Task HasActiveRoomAsync_ActiveRoomForAnotherQuiz_ReturnsFalse()
        {
            _db.Rooms.Add(new Room { QuizId = 2, HostId = "h", JoinCode = "111111", Status = RoomStatus.Waiting });
            await _db.SaveChangesAsync();

            Assert.False(await _repository.HasActiveRoomAsync(1));
        }
    }
}
