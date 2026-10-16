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
        Task<Question?> GetQuestionByIdAsync(int questionId);
        Task<AnswerOption?> GetAnswerOptionByIdAsync(int answerOptionId);
        Task AddQuestionAsync(Question question);
        Task DeleteQuestionAsync(Question question);
        Task AddAnswerOptionAsync(AnswerOption answerOption);
        Task DeleteAnswerOptionAsync(AnswerOption answerOption);
        Task SaveChangesAsync();
    }
}