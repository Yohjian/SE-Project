using QuattroLingo.Entities;

namespace QuattroLingo.Services
{
    public interface IQuizService
    {
        Task<List<Quiz>> GetMyQuizzesAsync(string userId);
        Task<Quiz?> GetByIdAsync(int id);
        Task<Quiz> CreateAsync(Quiz quiz, string userId);
        Task UpdateAsync(int id, Quiz updatedQuiz, string userId);
        Task DeleteAsync(int id, string userId);
    }
}