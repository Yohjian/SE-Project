using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Repositories;

namespace QuattroLingo.Services
{
    public class QuizService(IQuizRepository quizzes) : IQuizService
    {
        private const string QuizNotFound = "Quiz not found.";
        private const string QuizForbidden = "Forbidden.";
        private const string TitleRequired = "Quiz title is required.";
        private const string MinimumAnswers = "A question must have at least 2 answer options.";
        private const string OneCorrectAnswer = "A question must have exactly one correct answer.";

        public async Task<List<Quiz>> GetMyQuizzesAsync(string userId)
        {
            return await quizzes.GetByOwnerIdAsync(userId);
        }

        public async Task<Quiz?> GetByIdAsync(int id)
        {
            return await quizzes.GetByIdAsync(id);
        }

        public async Task<Quiz> CreateAsync(Quiz quiz, string userId)
        {
            ValidateQuiz(quiz);

            quiz.OwnerId = userId;
            await quizzes.AddAsync(quiz);

            return quiz;
        }

        public async Task UpdateAsync(int id, Quiz updatedQuiz, string userId)
        {
            var quiz = await quizzes.GetByIdAsync(id)
                ?? throw new NotFoundException(QuizNotFound);

            if (quiz.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            ValidateQuiz(updatedQuiz);

            quiz.Title = updatedQuiz.Title;
            quiz.Description = updatedQuiz.Description;
            quiz.Questions = updatedQuiz.Questions;

            await quizzes.UpdateAsync(quiz);
        }

        public async Task DeleteAsync(int id, string userId)
        {
            var quiz = await quizzes.GetByIdAsync(id)
                ?? throw new NotFoundException(QuizNotFound);

            if (quiz.OwnerId != userId)
                throw new ForbiddenException(QuizForbidden);

            await quizzes.DeleteAsync(quiz);
        }

        private static void ValidateQuiz(Quiz quiz)
        {
            if (string.IsNullOrWhiteSpace(quiz.Title))
                throw new ValidationException(TitleRequired);

            foreach (var question in quiz.Questions)
            {
                if (question.AnswerOptions.Count < 2)
                    throw new ValidationException(MinimumAnswers);

                if (question.AnswerOptions.Count(a => a.IsCorrect) != 1)
                    throw new ValidationException(OneCorrectAnswer);
            }
        }
    }
}