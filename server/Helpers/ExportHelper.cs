using System.Text.Json;
using QuattroLingo.Entities;

namespace QuattroLingo.Helpers
{
    public static class ExportHelper
    {
        public static async Task<MemoryStream> ToJsonStreamAsync(VocabularySet set)
        {
            var export = new // select data to export
            {
                Name = set.Name,
                CreatedAt = set.CreatedAt,
                cards = set.Cards.Select(c => new { Term = c.Term, Definition = c.Definition })
            };

            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(
                stream,
                export,
                new JsonSerializerOptions { WriteIndented = true });

            stream.Position = 0;
            return stream;
        }

        public static async Task<MemoryStream> ToCsvStreamAsync(VocabularySet set)
        {
            MemoryStream stream = new();
            StreamWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            await writer.WriteLineAsync("Term,Definition");
            foreach (VocabularyCard card in set.Cards)
            {
                String term = card.Term.SerializeEscapes();
                String definition = card.Definition.SerializeEscapes();
                await writer.WriteLineAsync($"\"{term}\",\"{definition}\"");
            }

            await writer.FlushAsync();
            stream.Position = 0;
            return stream;
        }
    }

}
