using QuattroLingo.DTOs.Request;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;

namespace QuattroLingo.Interfaces.Services
{
    public interface IPracticeService
    {
        Task<List<PracticeQuizSummary>> GetQuizzesAsync(
            int? category,
            int? language,
            QuizDifficulty? difficulty);

        Task<PracticeQuizResponse> GetQuizAsync(int id);

        Task<PracticeAnswerResponse> CheckAnswerAsync(
            PracticeAnswerRequest request);
    }
}