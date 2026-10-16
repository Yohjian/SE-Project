using System.Text.Json;
using QuattroLingo.Entities;
using QuattroLingo.Extensions;

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
                Cards = set.Cards.Select(c => new { Term = c.Term, Definition = c.Definition })
            };

            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(
                stream,
                export,
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });

            stream.Position = 0;
            return stream;
        }

        public static async Task<MemoryStream> ToCsvStreamAsync(VocabularySet set)
        {
            MemoryStream stream = new();

            await using (StreamWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                await writer.WriteLineAsync("Term,Definition");
                foreach (VocabularyCard card in set.Cards)
                {
                    string term = card.Term.ToCsvField();
                    string definition = card.Definition.ToCsvField();
                    await writer.WriteLineAsync($"\"{term}\",\"{definition}\"");
                }
            }

            stream.Position = 0;
            return stream;
        }
    }

}
