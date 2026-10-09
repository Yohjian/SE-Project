using QuattroLingo.DTOs.Request;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Repositories;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services;

public class QuizServiceTests
{
    private class FakeQuizRepository : IQuizRepository
    {
        public readonly List<Quiz> Quizzes = [];
        private int _nextQuizId = 1;

        public Task<List<Quiz>> GetByOwnerIdAsync(string ownerId) =>
            Task.FromResult(Quizzes
                .Where(q => q.OwnerId == ownerId)
                .ToList());

        public Task<Quiz?> GetByIdAsync(int id) =>
            Task.FromResult(Quizzes.FirstOrDefault(q => q.Id == id));

        private static void AssignId(object entity, int id) =>
            entity.GetType()
                .GetProperty("Id")!
                .GetSetMethod(nonPublic: true)!
                .Invoke(entity, [id]);

        public Task AddAsync(Quiz quiz)
        {
            AssignId(quiz, _nextQuizId++);
            Quizzes.Add(quiz);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Quiz quiz) =>
            Task.CompletedTask;

        public Task DeleteAsync(Quiz quiz)
        {
            Quizzes.Remove(quiz);
            return Task.CompletedTask;
        }

        public Task<Question?> GetQuestionByIdAsync(int questionId) =>
            Task.FromResult(
                Quizzes
                    .SelectMany(q => q.Questions)
                    .FirstOrDefault(q => q.Id == questionId)
            );

        public Task<AnswerOption?> GetAnswerOptionByIdAsync(int answerOptionId) =>
            Task.FromResult(
                Quizzes
                    .SelectMany(q => q.Questions)
                    .SelectMany(q => q.AnswerOptions)
                    .FirstOrDefault(a => a.Id == answerOptionId)
            );

        public Task AddQuestionAsync(Question question) =>
            Task.CompletedTask;

        public Task DeleteQuestionAsync(Question question) =>
            Task.CompletedTask;

        public Task AddAnswerOptionAsync(AnswerOption answerOption) =>
            Task.CompletedTask;

        public Task DeleteAnswerOptionAsync(AnswerOption answerOption) =>
            Task.CompletedTask;

        public Task SaveChangesAsync() =>
            Task.CompletedTask;
    }

    private readonly FakeQuizRepository _repo = new();
    private readonly QuizService _service;

    public QuizServiceTests()
    {
        _service = new QuizService(_repo);
    }

    private static QuestionRequest ValidQuestion()
    {
        return new QuestionRequest(
            "What is apple?",
            30,
            100,
            0,
            [
                new AnswerOptionRequest("Obuolys", true),
                new AnswerOptionRequest("Šuo", false)
            ]
        );
    }

    [Fact]
    public async Task Update_QuizOwnedBySomeoneElse_ThrowsForbidden()
    {
        var created = await _service.CreateAsync(
            "user-1",
            new CreateQuizRequest(
                "My Quiz",
                null,
                [ValidQuestion()]
            )
        );

        var request = new UpdateQuizRequest(
            "Changed Quiz",
            null,
            [ValidQuestion()]
        );

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateAsync("user-2", created.Id, request)
        );
    }

    [Fact]
    public async Task Delete_QuizOwnedBySomeoneElse_ThrowsForbidden()
    {
        var created = await _service.CreateAsync(
            "user-1",
            new CreateQuizRequest(
                "My Quiz",
                null,
                [ValidQuestion()]
            )
        );

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.DeleteAsync("user-2", created.Id)
        );

        Assert.Single(_repo.Quizzes);
    }

    [Fact]
    public async Task Create_QuestionWithNoCorrectAnswer_ThrowsValidation()
    {
        var question = new QuestionRequest(
            "What is apple?",
            30,
            100,
            0,
            [
                new AnswerOptionRequest("Obuolys", false),
                new AnswerOptionRequest("Šuo", false)
            ]
        );

        var request = new CreateQuizRequest(
            "My Quiz",
            null,
            [question]
        );

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync("user-1", request)
        );

        Assert.Empty(_repo.Quizzes);
    }

    [Fact]
    public async Task Create_QuestionWithMultipleCorrectAnswers_ThrowsValidation()
    {
        var question = new QuestionRequest(
            "What is apple?",
            30,
            100,
            0,
            [
                new AnswerOptionRequest("Obuolys", true),
                new AnswerOptionRequest("Šuo", true)
            ]
        );

        var request = new CreateQuizRequest(
            "My Quiz",
            null,
            [question]
        );

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync("user-1", request)
        );

        Assert.Empty(_repo.Quizzes);
    }

    [Fact]
    public async Task Create_QuestionWithLessThanTwoAnswers_ThrowsValidation()
    {
        var question = new QuestionRequest(
            "What is apple?",
            30,
            100,
            0,
            [
                new AnswerOptionRequest("Obuolys", true)
            ]
        );

        var request = new CreateQuizRequest(
            "My Quiz",
            null,
            [question]
        );

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync("user-1", request)
        );

        Assert.Empty(_repo.Quizzes);
    }

    [Fact]
    public async Task Create_QuestionWithEmptyText_ThrowsValidation()
    {
        var question = new QuestionRequest(
            "",
            30,
            100,
            0,
            [
                new AnswerOptionRequest("Obuolys", true),
                new AnswerOptionRequest("Šuo", false)
            ]
        );

        var request = new CreateQuizRequest(
            "My Quiz",
            null,
            [question]
        );

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync("user-1", request)
        );

        Assert.Empty(_repo.Quizzes);
    }

    [Fact]
    public async Task Create_QuestionWithEmptyAnswer_ThrowsValidation()
    {
        var question = new QuestionRequest(
            "What is apple?",
            30,
            100,
            0,
            [
                new AnswerOptionRequest("Obuolys", true),
                new AnswerOptionRequest("", false)
            ]
        );

        var request = new CreateQuizRequest(
            "My Quiz",
            null,
            [question]
        );

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync("user-1", request)
        );

        Assert.Empty(_repo.Quizzes);
    }

    [Fact]
    public async Task Create_QuestionWithExactlyOneCorrectAnswer_Succeeds()
    {
        var request = new CreateQuizRequest(
            "My Quiz",
            "Test quiz",
            [ValidQuestion()]
        );

        var result = await _service.CreateAsync("user-1", request);

        Assert.Equal("My Quiz", result.Title);

        var quiz = Assert.Single(_repo.Quizzes);
        var question = Assert.Single(quiz.Questions);

        Assert.Equal(2, question.AnswerOptions.Count);
        Assert.Single(question.AnswerOptions.Where(a => a.IsCorrect));
    }
}