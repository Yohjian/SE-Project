
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.DTOs.Request;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services
{
    public class PracticeServiceTests : IDisposable
    {
        private const string TestUserId = "user-1";
        private const string OtherUserId = "user-2";
        private const string SeedUserId = "seed-user";

        private readonly AppDbContext _context;
        private readonly PracticeService _service;

        public PracticeServiceTests()
        {
            _context = TestDb.Create();
            _service = new PracticeService(_context);

            _context.Users.AddRange(
                new ApplicationUser
                {
                    Id = TestUserId,
                    UserName = "test-user",
                    Email = "test@example.com"
                },
                new ApplicationUser
                {
                    Id = OtherUserId,
                    UserName = "other-user",
                    Email = "other@example.com"
                },
                new ApplicationUser
                {
                    Id = SeedUserId,
                    UserName = "practice-seed",
                    Email = "seed@example.com"
                }
            );

            _context.Quizzes.AddRange(
                CreateQuiz(
                    1, TestUserId, QuizKind.Practice,
                    1, 1, QuizDifficulty.Easy),

                CreateQuiz(
                    2, TestUserId, QuizKind.Practice,
                    2, 1, QuizDifficulty.Hard),

                CreateQuiz(
                    3, TestUserId, QuizKind.Custom,
                    null, null, null),

                CreateQuiz(
                    4, OtherUserId, QuizKind.Practice,
                    1, 1, QuizDifficulty.Easy),

                CreateQuiz(
                    5, SeedUserId, QuizKind.Practice,
                    1, 1, QuizDifficulty.Easy,
                    isPublic: true),

                CreateQuiz(
                    6, SeedUserId, QuizKind.Practice,
                    1, 1, QuizDifficulty.Easy),

                CreateQuiz(
                    7, OtherUserId, QuizKind.Practice,
                    1, 1, QuizDifficulty.Easy,
                    isPublic: true)
            );

            _context.SaveChanges();
            _context.ChangeTracker.Clear();
        }

        private static Quiz CreateQuiz(
            int id,
            string ownerId,
            QuizKind kind,
            int? categoryId,
            int? languageId,
            QuizDifficulty? difficulty,
            bool isPublic = false)
        {
            return new Quiz
            {
                Id = id,
                OwnerId = ownerId,
                Title = $"Quiz {id}",
                Kind = kind,
                CategoryId = categoryId,
                LanguageId = languageId,
                Difficulty = difficulty,
                IsPublic = isPublic,
                Questions =
                [
                    new Question
                    {
                        Id = id * 10,
                        Text = "What is apple?",
                        TimeLimitSeconds = 30,
                        Points = 100,
                        OrderIndex = 0,
                        AnswerOptions =
                        [
                            new AnswerOption
                            {
                                Id = id * 100 + 1,
                                Text = "Obuolys",
                                IsCorrect = true
                            },
                            new AnswerOption
                            {
                                Id = id * 100 + 2,
                                Text = "Šuo",
                                IsCorrect = false
                            }
                        ]
                    }
                ]
            };
        }

        [Fact]
        public async Task GetQuizzes_ReturnsOwnedAndPublicSeedQuizzes()
        {
            var result = await _service.GetQuizzesAsync(
                TestUserId, null, null, null);

            Assert.Equal(3, result.Count);
            Assert.Contains(result, q => q.Id == 1);
            Assert.Contains(result, q => q.Id == 2);
            Assert.Contains(result, q => q.Id == 5);
        }

        [Fact]
        public async Task GetQuizzes_ExcludesInaccessibleQuizzes()
        {
            var result = await _service.GetQuizzesAsync(
                TestUserId, null, null, null);

            Assert.DoesNotContain(result, q => q.Id == 3);
            Assert.DoesNotContain(result, q => q.Id == 4);
            Assert.DoesNotContain(result, q => q.Id == 6);
            Assert.DoesNotContain(result, q => q.Id == 7);
        }

        [Fact]
        public async Task GetQuizzes_FiltersByCategory()
        {
            var result = await _service.GetQuizzesAsync(
                TestUserId, 2, null, null);

            var quiz = Assert.Single(result);

            Assert.Equal(2, quiz.Id);
        }

        [Fact]
        public async Task GetQuizzes_FiltersByLanguageAndDifficulty()
        {
            var result = await _service.GetQuizzesAsync(
                TestUserId, null, 1, QuizDifficulty.Hard);

            var quiz = Assert.Single(result);

            Assert.Equal(2, quiz.Id);
        }

        [Fact]
        public async Task GetQuiz_DoesNotRevealCorrectAnswers()
        {
            var result = await _service.GetQuizAsync(
                1, TestUserId);

            var question = Assert.Single(result.Questions);

            Assert.Equal(2, question.AnswerOptions.Count);

            var json = JsonSerializer.Serialize(result);

            Assert.DoesNotContain("IsCorrect", json);
            Assert.DoesNotContain("CorrectAnswerOptionId", json);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(999)]
        public async Task GetQuiz_InaccessibleQuiz_ThrowsNotFound(
            int quizId)
        {
            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.GetQuizAsync(
                    quizId, TestUserId));
        }

        [Fact]
        public async Task GetQuiz_PublicSeedQuiz_ReturnsQuiz()
        {
            var result = await _service.GetQuizAsync(
                5, TestUserId);

            Assert.Equal(5, result.Id);
            Assert.Single(result.Questions);
        }

        [Fact]
        public async Task CheckAnswer_CorrectAnswer_ReturnsTrue()
        {
            var request = new PracticeAnswerRequest(
                1, 10, 101);

            var result = await _service.CheckAnswerAsync(
                request, TestUserId);

            Assert.True(result.IsCorrect);
            Assert.Equal(101, result.CorrectAnswerOptionId);
        }

        [Fact]
        public async Task CheckAnswer_WrongAnswer_ReturnsFalse()
        {
            var request = new PracticeAnswerRequest(
                1, 10, 102);

            var result = await _service.CheckAnswerAsync(
                request, TestUserId);

            Assert.False(result.IsCorrect);
            Assert.Equal(101, result.CorrectAnswerOptionId);
        }

        [Fact]
        public async Task CheckAnswer_AnswerFromAnotherQuestion_ThrowsValidation()
        {
            var request = new PracticeAnswerRequest(
                1, 10, 201);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.CheckAnswerAsync(
                    request, TestUserId));
        }

        [Fact]
        public async Task CheckAnswer_WrongQuizId_ThrowsNotFound()
        {
            var request = new PracticeAnswerRequest(
                2, 10, 101);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.CheckAnswerAsync(
                    request, TestUserId));
        }

        [Fact]
        public async Task CheckAnswer_InaccessibleQuiz_ThrowsNotFound()
        {
            var request = new PracticeAnswerRequest(
                4, 40, 401);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.CheckAnswerAsync(
                    request, TestUserId));
        }

        [Fact]
        public async Task CheckAnswer_PublicSeedQuiz_ReturnsCorrectResult()
        {
            var request = new PracticeAnswerRequest(
                5, 50, 501);

            var result = await _service.CheckAnswerAsync(
                request, TestUserId);

            Assert.True(result.IsCorrect);
            Assert.Equal(501, result.CorrectAnswerOptionId);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
