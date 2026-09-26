namespace QuattroLingo.Entity
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        public ICollection<Word> Words { get; set; } = [];
    }
}