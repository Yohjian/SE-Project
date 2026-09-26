namespace QuattroLingo.Models
{
    public class VocabularySet
    {
        public int Id { set; get; }
        public string Name { set; get; } = string.Empty;
        public string UserId { set; get; } = string.Empty;
        public DateTime CreatedAt { set; get; }
        public ICollection<VocabularyCard> Cards { get; set; } = [];
    }
}
