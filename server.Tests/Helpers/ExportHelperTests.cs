using System.Text;
using System.Text.Json;
using QuattroLingo.Entities;
using QuattroLingo.Extensions;
using QuattroLingo.Helpers;

namespace QuattroLingo.Tests.Helpers
{
    public class ExportHelperTests
    {
        private static VocabularySet MakeSet(params (string Term, string Definition)[] cards) => new()
        {
            Name = "Lietuvių kalba",
            CreatedAt = new DateTime(2026, 10, 1, 12, 30, 0, DateTimeKind.Utc),
            Cards = cards.Select(c => new VocabularyCard { Term = c.Term, Definition = c.Definition }).ToList()
        };

        private static async Task<string> ReadAllAsync(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        // ---- ToCsvField ----

        [Theory]
        [InlineData("labas", "\"labas\"")]
        [InlineData("", "\"\"")]
        [InlineData("a,b", "\"a,b\"")]
        [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
        [InlineData("line1\nline2", "\"line1\nline2\"")]
        public void ToCsvField_AnyString_ReturnsQuotedFieldWithInnerQuotesDoubled(string input, string expected)
        {
            Assert.Equal(expected, input.ToCsvField());
        }

        // ---- ToCsvStreamAsync ----

        [Fact]
        public async Task ToCsvStreamAsync_SetWithCards_ReturnsStreamRewoundToStart()
        {
            var stream = await ExportHelper.ToCsvStreamAsync(MakeSet(("a", "b")));

            Assert.Equal(0, stream.Position);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task ToCsvStreamAsync_EmptySet_WritesOnlyHeader()
        {
            var csv = await ReadAllAsync(await ExportHelper.ToCsvStreamAsync(MakeSet()));

            Assert.Equal("Term,Definition", csv.TrimEnd('\r', '\n'));
        }

        [Fact]
        public async Task ToCsvStreamAsync_SetWithCards_WritesHeaderThenOneRowPerCardInOrder()
        {
            var set = MakeSet(("labas", "hello"), ("ačiū", "thank you"));

            var lines = (await ReadAllAsync(await ExportHelper.ToCsvStreamAsync(set)))
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(new[] { "Term,Definition", "\"labas\",\"hello\"", "\"ačiū\",\"thank you\"" }, lines);
        }

        [Fact]
        public async Task ToCsvStreamAsync_NonAsciiCharacters_KeepsThemIntact()
        {
            var set = MakeSet(("žąsis", "goose"), ("Straße", "street"));

            var csv = await ReadAllAsync(await ExportHelper.ToCsvStreamAsync(set));

            Assert.Contains("\"žąsis\",\"goose\"", csv);
            Assert.Contains("\"Straße\",\"street\"", csv);
        }

        // ---- ToJsonStreamAsync ----

        [Fact]
        public async Task ToJsonStreamAsync_SetWithCards_ReturnsStreamRewoundToStart()
        {
            var stream = await ExportHelper.ToJsonStreamAsync(MakeSet(("a", "b")));

            Assert.Equal(0, stream.Position);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task ToJsonStreamAsync_SetWithCards_WritesCamelCaseNameDateAndCards()
        {
            var set = MakeSet(("labas", "hello"), ("ačiū", "thank you"));

            using var doc = JsonDocument.Parse(await ReadAllAsync(await ExportHelper.ToJsonStreamAsync(set)));
            var root = doc.RootElement;

            Assert.Equal("Lietuvių kalba", root.GetProperty("name").GetString());
            Assert.Equal(set.CreatedAt, root.GetProperty("createdAt").GetDateTime().ToUniversalTime());

            var cards = root.GetProperty("cards").EnumerateArray().ToList();
            Assert.Equal(2, cards.Count);
            Assert.Equal("labas", cards[0].GetProperty("term").GetString());
            Assert.Equal("hello", cards[0].GetProperty("definition").GetString());
            Assert.Equal("ačiū", cards[1].GetProperty("term").GetString());
        }

        [Fact]
        public async Task ToJsonStreamAsync_SetWithDatabaseIds_OmitsInternalIds()
        {
            var set = MakeSet(("labas", "hello"));
            set.Id = 42;
            set.UserId = "user-1";

            using var doc = JsonDocument.Parse(await ReadAllAsync(await ExportHelper.ToJsonStreamAsync(set)));
            var root = doc.RootElement;
            var card = root.GetProperty("cards")[0];

            Assert.False(root.TryGetProperty("id", out _));
            Assert.False(root.TryGetProperty("userId", out _));
            Assert.False(card.TryGetProperty("id", out _));
            Assert.False(card.TryGetProperty("setId", out _));
        }

        [Fact]
        public async Task ToJsonStreamAsync_EmptySet_WritesEmptyCardsArray()
        {
            using var doc = JsonDocument.Parse(await ReadAllAsync(await ExportHelper.ToJsonStreamAsync(MakeSet())));

            Assert.Equal(0, doc.RootElement.GetProperty("cards").GetArrayLength());
        }
    }
}
