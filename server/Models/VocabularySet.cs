namespace QuattroLingo.Models
{
    public class VocabularySet
    {
        public int ID { set; get; }
        public string Name { set; get; } = string.Empty;
        public string UserID { set; get; } = string.Empty;
        public DateTime CreatedAt { set; get; }
        public ICollection<VocabularyCard> Cards { get; set; } = [];
    }
}
