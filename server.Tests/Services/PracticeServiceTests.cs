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
        private readonly AppDbContext _context;
        private readonly PracticeService _service;

        public PracticeServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _service = new PracticeService(_context);

            _context.Quizzes.AddRange(
                CreateQuiz(1, QuizKind.Practice, 1, 1, QuizDifficulty.Easy),
                CreateQuiz(2, QuizKind.Practice, 2, 1, QuizDifficulty.Hard),
                CreateQuiz(3, QuizKind.Custom, null, null, null)
            );

            _context.SaveChanges();
        }

        private static Quiz CreateQuiz(
            int id,
            QuizKind kind,
            int? categoryId,
            int? languageId,
            QuizDifficulty? difficulty)
        {
            return new Quiz
            {
                Id = id,
                OwnerId = "user-1",
                Title = $"Quiz {id}",
                Kind = kind,
                CategoryId = categoryId,
                LanguageId = languageId,
                Difficulty = difficulty,
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
        public async Task GetQuizzes_ReturnsOnlyPracticeQuizzes()
        {
            var result = await _service.GetQuizzesAsync(
                null, null, null);

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, q => q.Id == 3);
        }

        [Fact]
        public async Task GetQuizzes_FiltersByCategory()
        {
            var result = await _service.GetQuizzesAsync(
                1, null, null);

            var quiz = Assert.Single(result);

            Assert.Equal(1, quiz.Id);
        }

        [Fact]
        public async Task GetQuizzes_FiltersByLanguageAndDifficulty()
        {
            var result = await _service.GetQuizzesAsync(
                null, 1, QuizDifficulty.Hard);

            var quiz = Assert.Single(result);

            Assert.Equal(2, quiz.Id);
        }

        [Fact]
        public async Task GetQuiz_DoesNotRevealCorrectAnswers()
        {
            var result = await _service.GetQuizAsync(1);

            var question = Assert.Single(result.Questions);

            Assert.Equal(2, question.AnswerOptions.Count);

            var json = JsonSerializer.Serialize(result);

            Assert.DoesNotContain("IsCorrect", json);
            Assert.DoesNotContain("CorrectAnswerOptionId", json);
        }

        [Fact]
        public async Task GetQuiz_CustomQuiz_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.GetQuizAsync(3));
        }

        [Fact]
        public async Task CheckAnswer_CorrectAnswer_ReturnsTrue()
        {
            var request = new PracticeAnswerRequest(
                1, 10, 101);

            var result = await _service.CheckAnswerAsync(request);

            Assert.True(result.IsCorrect);
            Assert.Equal(101, result.CorrectAnswerOptionId);
        }

        [Fact]
        public async Task CheckAnswer_WrongAnswer_ReturnsFalse()
        {
            var request = new PracticeAnswerRequest(
                1, 10, 102);

            var result = await _service.CheckAnswerAsync(request);

            Assert.False(result.IsCorrect);
            Assert.Equal(101, result.CorrectAnswerOptionId);
        }

        [Fact]
        public async Task CheckAnswer_AnswerFromAnotherQuestion_ThrowsValidation()
        {
            var request = new PracticeAnswerRequest(
                1, 10, 201);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.CheckAnswerAsync(request));
        }

        [Fact]
        public async Task CheckAnswer_WrongQuizId_ThrowsNotFound()
        {
            var request = new PracticeAnswerRequest(
                2, 10, 101);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.CheckAnswerAsync(request));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}