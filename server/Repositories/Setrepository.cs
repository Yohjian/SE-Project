using Microsoft.EntityFrameworkCore;
using QuattroLingo.Data;
using QuattroLingo.Entities;

namespace QuattroLingo.Repositories
{
    public class SetRepository(AppDbContext db) : ISetRepository
    {
        public Task<List<SetListItem>> GetByUserAsync(string userId) =>
            db.Sets
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new SetListItem(s.Id, s.Name, s.Cards.Count, s.CreatedAt))
                .ToListAsync();

        public Task<VocabularySet?> GetOwnedWithCardsAsync(string userId, int setId) =>
            db.Sets
                .AsNoTracking()
                .Include(s => s.Cards)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Id == setId);

        public Task<bool> ExistsForUserAsync(string userId, int setId) =>
            db.Sets.AnyAsync(s => s.UserId == userId && s.Id == setId);

        public Task<List<VocabularyCard>> GetCardsAsync(int setId) =>
            db.Cards
                .AsNoTracking()
                .Where(c => c.SetId == setId)
                .ToListAsync();

        public async Task<VocabularySet> AddAsync(VocabularySet set)
        {
            db.Sets.Add(set);
            await db.SaveChangesAsync();
            return set;
        }

        public async Task<VocabularyCard> AddCardAsync(VocabularyCard card)
        {
            db.Cards.Add(card);
            await db.SaveChangesAsync();
            return card;
        }
    }
}