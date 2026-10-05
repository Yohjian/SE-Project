using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Repositories;
using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;

namespace QuattroLingo.Services
{
    public class QuizService(IQuizRepository quizzes) : IQuizService
    {
        private const string QuizNotFound = "Quiz not found.";
        private const string QuizForbidden = "Forbidden.";
        private const string TitleRequired = "Quiz title is required.";
        private const string MinimumAnswers = "A question must have at least 2 answer options.";
        private const string OneCorrectAnswer = "A question must have exactly one correct answer.";

        public async Task<List<QuizSummary>> GetMyQuizzesAsync(string userId)
        {
            var items = await quizzes.GetByOwnerIdAsync(userId);
            return items.Select(q => new QuizSummary(q.Id, q.Title, q.Description)).ToList();
        }

        public async Task<QuizEditorResponse> GetByIdAsync(string userId, int quizId)
        {
            var quiz = await quizzes.GetByIdAsync(quizId)
                ?? throw new NotFoundException(QuizNotFound);

            if (quiz.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            return ToResponse(quiz);
        }

        public async Task<QuizEditorResponse> CreateAsync(string userId, CreateQuizRequest request)
        {
            ValidateQuiz(request.Title, request.Questions);

            var quiz = new Quiz
            {
                OwnerId = userId,
                Title = request.Title,
                Description = request.Description,
                Questions = request.Questions.Select(ToQuestion).ToList()
            };

            await quizzes.AddAsync(quiz);

            return ToResponse(quiz);
        }

        public async Task<QuizEditorResponse> UpdateAsync(string userId, int quizId, UpdateQuizRequest request)
        {
            var quiz = await quizzes.GetByIdAsync(quizId)
                ?? throw new NotFoundException(QuizNotFound);

            if (quiz.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            ValidateQuiz(request.Title, request.Questions);

            quiz.Title = request.Title;
            quiz.Description = request.Description;
            quiz.Questions = request.Questions.Select(ToQuestion).ToList();

            await quizzes.UpdateAsync(quiz);

            return ToResponse(quiz);
        }

        public async Task DeleteAsync(string userId, int quizId)
        {
            var quiz = await quizzes.GetByIdAsync(quizId)
                ?? throw new NotFoundException(QuizNotFound);

            if (quiz.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            await quizzes.DeleteAsync(quiz);
        }

        private static void ValidateQuiz(string title, List<QuestionRequest> questions)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ValidationException(TitleRequired);

            foreach (var question in questions)
            {
                if (question.AnswerOptions.Count < 2)
                    throw new ValidationException(MinimumAnswers);

                if (question.AnswerOptions.Count(a => a.IsCorrect) != 1)
                    throw new ValidationException(OneCorrectAnswer);
            }
        }
         private static Question ToQuestion(QuestionRequest request)
        {
            return new Question
            {
                Text = request.Text,
                TimeLimitSeconds = request.TimeLimitSeconds,
                Points = request.Points,
                OrderIndex = request.OrderIndex,

                AnswerOptions = request.AnswerOptions
                    .Select(a => new AnswerOption
                    {
                        Text = a.Text,
                        IsCorrect = a.IsCorrect
                    })
                    .ToList()
            };
        }

        private static QuizEditorResponse ToResponse(Quiz quiz)
        {
            return new QuizEditorResponse(
                quiz.Id,
                quiz.Title,
                quiz.Description,

                quiz.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuestionResponse(
                        q.Id,
                        q.Text,
                        q.TimeLimitSeconds,
                        q.Points,
                        q.OrderIndex,

                        q.AnswerOptions
                            .Select(a => new AnswerOptionResponse(
                                a.Id,
                                a.Text,
                                a.IsCorrect))
                            .ToList()))
                    .ToList());
        }
    }
}