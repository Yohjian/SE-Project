using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;

namespace QuattroLingo.Interfaces.Services
{
    public interface IPracticeService
    {
        Task<List<PracticeQuizSummary>> GetQuizzesAsync(
            string userId,
            int? category,
            int? language,
            QuizDifficulty? difficulty);

        Task<PracticeQuizResponse> GetQuizAsync(int id, string userId);

        Task<PracticeAnswerResponse> CheckAnswerAsync(
            PracticeAnswerRequest request, string userId);

        Task<PracticeFiltersResponse> GetFiltersAsync();
    }
}