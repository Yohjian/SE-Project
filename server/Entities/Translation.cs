namespace QuattroLingo.Entity
{
    public class Translation
    {
        public int Id { get; set; }

        public int WordId { get; set; }
        public Word Word { get; set; } = null!;

        public int LanguageId { get; set; }
        public Language Language { get; set; } = null!;

        public required string Text { get; set; }
    }
}