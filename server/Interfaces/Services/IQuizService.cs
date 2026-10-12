using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;

namespace QuattroLingo.Services
{
    public interface IQuizService
    {
        Task<List<QuizSummary>> GetMyQuizzesAsync(string userId);
        Task<QuizEditorResponse> GetByIdAsync(string userId, int quizId);
        Task<QuizEditorResponse> CreateAsync(string userId, CreateQuizRequest request);
        Task<QuizEditorResponse> UpdateAsync(string userId, int quizId, UpdateQuizRequest request);
        Task DeleteAsync(string userId, int quizId);
        Task<QuestionResponse> AddQuestionAsync(
            string userId,
            int quizId,
            QuestionRequest request);

        Task<QuestionResponse> UpdateQuestionAsync(
            string userId,
            int questionId,
            QuestionRequest request);

        Task DeleteQuestionAsync(
            string userId,
            int questionId);

        Task<AnswerOptionResponse> AddAnswerOptionAsync(
            string userId,
            int questionId,
            AnswerOptionRequest request);

        Task<AnswerOptionResponse> UpdateAnswerOptionAsync(
            string userId,
            int answerOptionId,
            AnswerOptionRequest request);

        Task DeleteAnswerOptionAsync(
            string userId,
            int answerOptionId);
    }
}