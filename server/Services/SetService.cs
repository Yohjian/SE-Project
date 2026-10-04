using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;
using QuattroLingo.Exceptions;
using QuattroLingo.Repositories;

namespace QuattroLingo.Services
{
    public class SetService(ISetRepository sets) : ISetService
    {
        private const string SetNotFound = "Set not found.";

        public async Task<List<SetSummary>> GetAllAsync(string userId)
        {
            var items = await sets.GetByUserAsync(userId);
            return items
                .Select(i => new SetSummary(i.Id, i.Name, i.CardCount, i.CreatedAt))
                .ToList();
        }

        public async Task<SetContents> GetAsync(string userId, int setId)
        {
            var set = await sets.GetOwnedWithCardsAsync(userId, setId)
                ?? throw new NotFoundException(SetNotFound);

            return new SetContents(
                set.Id,
                set.Name,
                set.Cards.Select(c => new CardResponse(c.Id, c.Term, c.Definition)).ToList());
        }

        public async Task<SetSummary> CreateAsync(string userId, string name)
        {
            var set = await sets.AddAsync(new VocabularySet
            {
                UserId = userId,
                Name = name,
                CreatedAt = DateTime.UtcNow,
                Cards = []
            });

            return new SetSummary(set.Id, set.Name, 0, set.CreatedAt);
        }

        public async Task<CardResponse> AddCardAsync(string userId, int setId, string term, string definition)
        {
            await EnsureOwnedAsync(userId, setId);

            var card = await sets.AddCardAsync(new VocabularyCard
            {
                Term = term,
                Definition = definition,
                SetId = setId
            });

            return new CardResponse(card.Id, card.Term, card.Definition);
        }

        public async Task<List<CardResponse>> GetCardsAsync(string userId, int setId)
        {
            await EnsureOwnedAsync(userId, setId);

            var cards = await sets.GetCardsAsync(setId);
            return cards.Select(c => new CardResponse(c.Id, c.Term, c.Definition)).ToList();
        }

        private async Task EnsureOwnedAsync(string userId, int setId)
        {
            if (!await sets.ExistsForUserAsync(userId, setId))
                throw new NotFoundException(SetNotFound);
        }
    }
}