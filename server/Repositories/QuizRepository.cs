using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;

namespace QuattroLingo.Repositories
{
    public class QuizRepository : IQuizRepository
    {
        private readonly AppDbContext _context;

        public QuizRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Quiz>> GetByOwnerIdAsync(string ownerId)
        {
            return await _context.Quizzes
                .Where(q => q.OwnerId == ownerId)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .ToListAsync();
        }

        public async Task<Quiz?> GetByIdAsync(int id)
        {
            return await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task AddAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Quiz quiz)
        {
            _context.Quizzes.Update(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Quiz quiz)
        {
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
        }
    }
}