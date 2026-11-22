using System.Text.Json;
using QuattroLingo.DTOs.Response;
using QuattroLingo.Entities;
using QuattroLingo.Enums;
using QuattroLingo.Exceptions;
using QuattroLingo.Helpers;
using QuattroLingo.Interfaces.Repositories;
using QuattroLingo.Interfaces.Services;
using QuattroLingo.Models;

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
                set.Cards.Select(c => new CardResponse(c.Id, c.Term, c.Definition, c.Meta.ToSerializable())).ToList());
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

        public async Task<CardResponse> AddCardAsync(string userId, int setId, string term, string definition, Dictionary<string, JsonElement>? metadata = null)
        {
            await EnsureOwnedAsync(userId, setId);

            var existing = await sets.GetCardsAsync(setId);
            if (existing.Any(c => string.Equals(c.Term, term, StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException($"A card with the term \"{term}\" already exists in this set.");

            var card = new VocabularyCard { Term = term, Definition = definition, SetId = setId };

            if (metadata is not null)
                foreach (var (key, value) in metadata)
                    card.Meta.Set(key, value);

            var saved = await sets.AddCardAsync(card);
            return new CardResponse(saved.Id, saved.Term, saved.Definition, saved.Meta.ToSerializable());
        }

        public async Task<List<CardResponse>> GetCardsAsync(string userId, int setId, string? sortBy = null, bool sortAsc = true)
        {
            await EnsureOwnedAsync(userId, setId);
            var cards = await sets.GetCardsAsync(setId);

            IEnumerable<VocabularyCard> ordered = sortBy is null
                ? cards
                : sortAsc
                    ? cards.OrderBy(c => GetSortValue(c.Meta, sortBy))
                    : cards.OrderByDescending(c => GetSortValue(c.Meta, sortBy));

            return ordered.Select(c => new CardResponse(c.Id, c.Term, c.Definition, c.Meta.ToSerializable())).ToList();
        }

        public async Task SetSetMetadataAsync(string userId, int setId, string key, JsonElement value)
        {
            var set = await sets.GetOwnedWithCardsAsync(userId, setId)
                ?? throw new NotFoundException(SetNotFound);
            set.Meta.Set(key, value);
            await sets.UpdateSetAsync(set);
        }

        public async Task SetCardMetadataAsync(string userId, int setId, int cardId, string key, JsonElement value)
        {
            await EnsureOwnedAsync(userId, setId);
            var card = await sets.GetCardAsync(setId, cardId)
                ?? throw new NotFoundException("Card not found.");
            card.Meta.Set(key, value);
            await sets.UpdateCardAsync(card);
        }

        private static IComparable GetSortValue(Metadata metadata, string key)
        {
            if (!metadata.ContainsKey(key)) return 0;
            return metadata[key] switch
            {
                MetadataEntry<int> e => (IComparable)e.Value,
                MetadataEntry<double> e => e.Value,
                MetadataEntry<string> e => e.Value,
                MetadataEntry<JsonElement> e => e.Value.ValueKind == JsonValueKind.Number
                    ? e.Value.GetDouble()
                    : e.Value.GetString() ?? "",
                _ => 0
            };
        }

        public async Task<DownloadFile> DownloadSetAsync(string userId, int setId, FileType ft)
        {
            VocabularySet set = await sets.GetOwnedWithCardsAsync(userId, setId)
                ?? throw new NotFoundException(SetNotFound);

            return ft switch
            {
                FileType.Json => new DownloadFile(await ExportHelper.ToJsonStreamAsync(set), "application/json", $"{set.Name}.json"),
                FileType.Csv => new DownloadFile(await ExportHelper.ToCsvStreamAsync(set), "text/csv", $"{set.Name}.csv"),
                _ => throw new UnsupportedFileTypeException($"Download of filetype {ft} not supported.")
            };
        }

        public async Task DeleteSetAsync(string userId, int setId)
        {
            await EnsureOwnedAsync(userId, setId);
            await sets.DeleteSetAsync(setId);
        }

        public async Task DeleteCardAsync(string userId, int setId, int cardId)
        {
            await EnsureOwnedAsync(userId, setId);
            var cards = await sets.GetCardsAsync(setId);
            if (cards.All(c => c.Id != cardId))
                throw new NotFoundException("Card not found.");
            await sets.DeleteCardAsync(cardId);
        }

        private async Task EnsureOwnedAsync(string userId, int setId)
        {
            if (!await sets.ExistsForUserAsync(userId, setId))
                throw new NotFoundException(SetNotFound);
        }
    }
}
