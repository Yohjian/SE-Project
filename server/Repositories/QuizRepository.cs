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

        public async Task<Question?> GetQuestionByIdAsync(int questionId)
        {
            return await _context.Questions
                .Include(q => q.Quiz)
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        public async Task<AnswerOption?> GetAnswerOptionByIdAsync(int answerOptionId)
        {
            return await _context.AnswerOptions
                .Include(a => a.Question)
                .ThenInclude(q => q!.Quiz)
                .FirstOrDefaultAsync(a => a.Id == answerOptionId);
        }

        public async Task AddQuestionAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteQuestionAsync(Question question)
        {
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
        }

        public async Task AddAnswerOptionAsync(AnswerOption answerOption)
        {
            _context.AnswerOptions.Add(answerOption);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAnswerOptionAsync(AnswerOption answerOption)
        {
            _context.AnswerOptions.Remove(answerOption);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}