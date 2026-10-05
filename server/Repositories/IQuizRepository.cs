using QuattroLingo.Entities;

namespace QuattroLingo.Repositories
{
    public interface IQuizRepository
    {
        Task<List<Quiz>> GetByOwnerIdAsync(string ownerId);
        Task<Quiz?> GetByIdAsync(int id);
        Task AddAsync(Quiz quiz);
        Task UpdateAsync(Quiz quiz);
        Task DeleteAsync(Quiz quiz);
    }
}