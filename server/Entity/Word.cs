namespace QuattroLingo.Entity
{
    public class Word
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string? ImageUrl { get; set; }  // for picture-matching games

        public ICollection<Translation> Translations { get; set; } = [];
    }
}