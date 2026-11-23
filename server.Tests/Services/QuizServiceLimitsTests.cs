using NSubstitute;
using QuattroLingo.DTOs.Request;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Helpers;
using QuattroLingo.Interfaces.Repositories;
using QuattroLingo.Services;

namespace QuattroLingo.Tests.Services
{
    public class QuizServiceLimitsTests
    {
        private readonly IQuizRepository _repo = Substitute.For<IQuizRepository>();
        private readonly QuizService _service;

        public QuizServiceLimitsTests()
        {
            _service = new QuizService(_repo);
        }

        private static List<AnswerOptionRequest> Options(int count = 2) =>
            Enumerable.Range(0, count)
                .Select(i => new AnswerOptionRequest($"Option {i}", i == 0))
                .ToList();

        private static QuestionRequest Question(
            string text = "Question?", int time = 30, int points = 100, int options = 2) =>
            new(text, time, points, 0, Options(options));

        private static string Long(int n) => new('x', n);

        private Task Create(string title = "Quiz", string? description = null, params QuestionRequest[] questions) =>
            _service.CreateAsync("user-1", new CreateQuizRequest(title, description, [.. questions]));

        [Fact]
        public async Task Create_AtAllMaximums_Succeeds()
        {
            var questions = Enumerable.Range(0, Limits.QuestionsPerQuiz)
                .Select(_ => Question(Long(Limits.QuestionTextMax), Limits.TimeLimitMaxSeconds,
                    Limits.PointsMax, Limits.AnswerOptionsMax))
                .ToArray();

            await Create(Long(Limits.QuizTitleMax), Long(Limits.QuizDescriptionMax), questions);

            await _repo.Received(1).AddAsync(Arg.Any<Quiz>());
        }

        [Fact]
        public async Task Create_TitleTooLong_ThrowsValidation() =>
            await Assert.ThrowsAsync<ValidationException>(() => Create(Long(Limits.QuizTitleMax + 1)));

        [Fact]
        public async Task Create_DescriptionTooLong_ThrowsValidation() =>
            await Assert.ThrowsAsync<ValidationException>(
                () => Create("Quiz", Long(Limits.QuizDescriptionMax + 1)));

        [Fact]
        public async Task Create_TooManyQuestions_ThrowsValidation()
        {
            var questions = Enumerable.Range(0, Limits.QuestionsPerQuiz + 1)
                .Select(_ => Question()).ToArray();

            await Assert.ThrowsAsync<ValidationException>(() => Create("Quiz", null, questions));
        }

        [Fact]
        public async Task Create_QuestionTextTooLong_ThrowsValidation() =>
            await Assert.ThrowsAsync<ValidationException>(
                () => Create("Quiz", null, Question(Long(Limits.QuestionTextMax + 1))));

        [Fact]
        public async Task Create_TooManyOptions_ThrowsValidation() =>
            await Assert.ThrowsAsync<ValidationException>(
                () => Create("Quiz", null, Question(options: Limits.AnswerOptionsMax + 1)));

        [Fact]
        public async Task Create_AnswerTextTooLong_ThrowsValidation()
        {
            var q = new QuestionRequest("Q?", 30, 100, 0, [
                new AnswerOptionRequest(Long(Limits.AnswerTextMax + 1), true),
                new AnswerOptionRequest("b", false)]);

            await Assert.ThrowsAsync<ValidationException>(() => Create("Quiz", null, q));
        }

        [Theory]
        [InlineData(Limits.TimeLimitMinSeconds - 1)]
        [InlineData(Limits.TimeLimitMaxSeconds + 1)]
        public async Task Create_TimeLimitOutOfRange_ThrowsValidation(int seconds) =>
            await Assert.ThrowsAsync<ValidationException>(
                () => Create("Quiz", null, Question(time: seconds)));

        [Theory]
        [InlineData(Limits.PointsMin - 1)]
        [InlineData(Limits.PointsMax + 1)]
        public async Task Create_PointsOutOfRange_ThrowsValidation(int points) =>
            await Assert.ThrowsAsync<ValidationException>(
                () => Create("Quiz", null, Question(points: points)));

        [Fact]
        public async Task Create_UserAlreadyAtQuizLimit_ThrowsValidationAndSavesNothing()
        {
            _repo.CountByOwnerAsync("user-1").Returns(Limits.QuizzesPerUser);

            await Assert.ThrowsAsync<ValidationException>(() => Create());

            await _repo.DidNotReceive().AddAsync(Arg.Any<Quiz>());
        }

        [Fact]
        public async Task Create_UserBelowQuizLimit_Succeeds()
        {
            _repo.CountByOwnerAsync("user-1").Returns(Limits.QuizzesPerUser - 1);

            await Create();

            await _repo.Received(1).AddAsync(Arg.Any<Quiz>());
        }

        [Fact]
        public async Task CopyPublicQuiz_UserAtQuizLimit_ThrowsValidation()
        {
            _repo.GetByIdAsync(5).Returns(new Quiz { Id = 5, OwnerId = "other", Title = "Pub", IsPublic = true });
            _repo.CountByOwnerAsync("user-1").Returns(Limits.QuizzesPerUser);

            await Assert.ThrowsAsync<ValidationException>(() => _service.CopyPublicQuizAsync("user-1", 5));

            await _repo.DidNotReceive().AddAsync(Arg.Any<Quiz>());
        }

        [Fact]
        public async Task AddQuestion_QuizAtQuestionLimit_ThrowsValidation()
        {
            _repo.GetByIdAsync(1).Returns(new Quiz { Id = 1, OwnerId = "user-1", Title = "Quiz" });
            _repo.CountQuestionsAsync(1).Returns(Limits.QuestionsPerQuiz);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddQuestionAsync("user-1", 1, Question()));

            await _repo.DidNotReceive().AddQuestionAsync(Arg.Any<Question>());
        }

        [Fact]
        public async Task AddQuestion_BelowQuestionLimit_Succeeds()
        {
            _repo.GetByIdAsync(1).Returns(new Quiz { Id = 1, OwnerId = "user-1", Title = "Quiz" });
            _repo.CountQuestionsAsync(1).Returns(Limits.QuestionsPerQuiz - 1);

            await _service.AddQuestionAsync("user-1", 1, Question());

            await _repo.Received(1).AddQuestionAsync(Arg.Any<Question>());
        }

        private Question QuestionWithOptions(int count)
        {
            var q = new Question
            {
                Id = 7,
                QuizId = 1,
                Text = "Q?",
                Quiz = new Quiz { Id = 1, OwnerId = "user-1", Title = "Quiz" },
                AnswerOptions = Enumerable.Range(0, count)
                    .Select(i => new AnswerOption { Id = i + 1, Text = $"o{i}", IsCorrect = i == 0 })
                    .ToList<AnswerOption>()
            };
            _repo.GetQuestionByIdAsync(7).Returns(q);
            return q;
        }

        [Fact]
        public async Task AddAnswerOption_QuestionAtOptionLimit_ThrowsValidation()
        {
            QuestionWithOptions(Limits.AnswerOptionsMax);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddAnswerOptionAsync("user-1", 7, new AnswerOptionRequest("new", false)));
        }

        [Fact]
        public async Task AddAnswerOption_TextTooLong_ThrowsValidation()
        {
            QuestionWithOptions(2);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.AddAnswerOptionAsync("user-1", 7,
                    new AnswerOptionRequest(Long(Limits.AnswerTextMax + 1), false)));
        }

        [Fact]
        public async Task UpdateAnswerOption_TextTooLong_ThrowsValidation()
        {
            var q = QuestionWithOptions(2);
            var option = q.AnswerOptions.First();
            option.Question = q;
            _repo.GetAnswerOptionByIdAsync(1).Returns(option);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.UpdateAnswerOptionAsync("user-1", 1,
                    new AnswerOptionRequest(Long(Limits.AnswerTextMax + 1), true)));
        }
    }
}
