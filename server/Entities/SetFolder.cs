
namespace QuattroLingo.Entities
{
    public class SetFolder
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ICollection<VocabularySet> Sets { get; set; } = new List<VocabularySet>();
    }
}
