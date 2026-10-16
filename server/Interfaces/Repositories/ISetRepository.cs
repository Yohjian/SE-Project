using QuattroLingo.Entities;

namespace QuattroLingo.Repositories
{
    public interface ISetRepository
    {
        Task<List<SetListItem>> GetByUserAsync(string userId);

        Task<VocabularySet?> GetOwnedWithCardsAsync(string userId, int setId);

        Task<bool> ExistsForUserAsync(string userId, int setId);

        Task<List<VocabularyCard>> GetCardsAsync(int setId);

        Task<VocabularySet> AddAsync(VocabularySet set);
        Task<VocabularyCard> AddCardAsync(VocabularyCard card);
    }
}