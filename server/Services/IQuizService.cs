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
    }
}