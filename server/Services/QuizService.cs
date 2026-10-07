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
        private const string QuestionNotFound = "Question not found.";
        private const string AnswerOptionNotFound = "Answer option not found.";
        private const string QuizForbidden = "Forbidden.";
        private const string TitleRequired = "Quiz title is required.";
        private const string MinimumAnswers = "A question must have at least 2 answer options.";
        private const string OneCorrectAnswer = "A question must have exactly one correct answer.";
        private const string CannotDeleteAnswer = "A question must have at least 2 answer options.";
        private const string CannotChangeCorrectAnswer = "A question must have exactly one correct answer.";
        private const string QuestionTextRequired = "Question text is required.";
        private const string AnswerTextRequired = "Answer text is required.";   

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

        public async Task<QuestionResponse> AddQuestionAsync(string userId, int quizId, QuestionRequest request)
        {
            var quiz = await quizzes.GetByIdAsync(quizId)
                ?? throw new NotFoundException(QuizNotFound);

            if (quiz.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            ValidateQuestion(request);

            var question = ToQuestion(request);
            question.QuizId = quizId;

            await quizzes.AddQuestionAsync(question);

            return ToQuestionResponse(question);
        }

        public async Task<QuestionResponse> UpdateQuestionAsync(string userId, int questionId, QuestionRequest request)
        {
            var question = await quizzes.GetQuestionByIdAsync(questionId)
                ?? throw new NotFoundException(QuestionNotFound);

            if (question.Quiz?.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            ValidateQuestion(request);

            question.Text = request.Text;
            question.TimeLimitSeconds = request.TimeLimitSeconds;
            question.Points = request.Points;
            question.OrderIndex = request.OrderIndex;
            question.AnswerOptions = request.AnswerOptions.Select(a => new AnswerOption
            {
                Text = a.Text,
                IsCorrect = a.IsCorrect
            }).ToList();

            await quizzes.SaveChangesAsync();

            return ToQuestionResponse(question);
        }

        public async Task DeleteQuestionAsync(string userId, int questionId)
        {
            var question = await quizzes.GetQuestionByIdAsync(questionId)
                ?? throw new NotFoundException(QuestionNotFound);

            if (question.Quiz?.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            await quizzes.DeleteQuestionAsync(question);
        }

        public async Task<AnswerOptionResponse> AddAnswerOptionAsync(string userId, int questionId, AnswerOptionRequest request)
        {
            var question = await quizzes.GetQuestionByIdAsync(questionId)
                ?? throw new NotFoundException(QuestionNotFound);

            if (question.Quiz?.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            if (request.IsCorrect && question.AnswerOptions.Any(a => a.IsCorrect))
                throw new ValidationException(CannotChangeCorrectAnswer);

            var answerOption = new AnswerOption
            {
                QuestionId = questionId,
                Text = request.Text,
                IsCorrect = request.IsCorrect
            };

            await quizzes.AddAnswerOptionAsync(answerOption);

            return new AnswerOptionResponse(answerOption.Id, answerOption.Text, answerOption.IsCorrect);
        }

        public async Task<AnswerOptionResponse> UpdateAnswerOptionAsync(string userId, int answerOptionId, AnswerOptionRequest request)
        {
            var answerOption = await quizzes.GetAnswerOptionByIdAsync(answerOptionId)
                ?? throw new NotFoundException(AnswerOptionNotFound);

            if (answerOption.Question?.Quiz?.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            var question = answerOption.Question;

            if (request.IsCorrect && !answerOption.IsCorrect && question.AnswerOptions.Any(a => a.IsCorrect))
                throw new ValidationException(CannotChangeCorrectAnswer);

            if (!request.IsCorrect && answerOption.IsCorrect)
                throw new ValidationException(CannotChangeCorrectAnswer);

            answerOption.Text = request.Text;
            answerOption.IsCorrect = request.IsCorrect;

            await quizzes.SaveChangesAsync();

            return new AnswerOptionResponse(answerOption.Id, answerOption.Text, answerOption.IsCorrect);
        }

        public async Task DeleteAnswerOptionAsync(string userId, int answerOptionId)
        {
            var answerOption = await quizzes.GetAnswerOptionByIdAsync(answerOptionId)
                ?? throw new NotFoundException(AnswerOptionNotFound);

            if (answerOption.Question?.Quiz?.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            if (answerOption.Question.AnswerOptions.Count <= 2)
                throw new ValidationException(CannotDeleteAnswer);

            if (answerOption.IsCorrect)
                throw new ValidationException(CannotChangeCorrectAnswer);

            await quizzes.DeleteAnswerOptionAsync(answerOption);
        }

        private static void ValidateQuiz(string title, List<QuestionRequest> questions)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ValidationException(TitleRequired);

            foreach (var question in questions)
                ValidateQuestion(question);
        }

        private static void ValidateQuestion(QuestionRequest question)
        {
            if (string.IsNullOrWhiteSpace(question.Text))
                throw new ValidationException(QuestionTextRequired);

            if (question.AnswerOptions.Count < 2)
                throw new ValidationException(MinimumAnswers);

            if (question.AnswerOptions.Any(a => string.IsNullOrWhiteSpace(a.Text)))
                throw new ValidationException(AnswerTextRequired);

            if (question.AnswerOptions.Count(a => a.IsCorrect) != 1)
                throw new ValidationException(OneCorrectAnswer);
        }

        private static Question ToQuestion(QuestionRequest request)
        {
            return new Question
            {
                Text = request.Text,
                TimeLimitSeconds = request.TimeLimitSeconds,
                Points = request.Points,
                OrderIndex = request.OrderIndex,
                AnswerOptions = request.AnswerOptions.Select(a => new AnswerOption
                {
                    Text = a.Text,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };
        }

        private static QuizEditorResponse ToResponse(Quiz quiz)
        {
            return new QuizEditorResponse(quiz.Id, quiz.Title, quiz.Description, quiz.Questions.OrderBy(q => q.OrderIndex)
                .Select(q => new QuestionResponse(q.Id, q.Text, q.TimeLimitSeconds, q.Points, q.OrderIndex,
                    q.AnswerOptions.Select(a => new AnswerOptionResponse(a.Id, a.Text, a.IsCorrect)).ToList()))
                .ToList());
        }

        private static QuestionResponse ToQuestionResponse(Question question)
        {
            return new QuestionResponse(question.Id, question.Text, question.TimeLimitSeconds, question.Points, question.OrderIndex,
                question.AnswerOptions.Select(a => new AnswerOptionResponse(a.Id, a.Text, a.IsCorrect)).ToList());
        }
    }
}