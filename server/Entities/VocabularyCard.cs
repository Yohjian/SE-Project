
namespace QuattroLingo.Entities
{
    public class VocabularyCard()
    {
        public int Id { protected set; get; }
        public string Term { set; get; } = string.Empty;
        public string Definition { set; get; } = string.Empty;
        public int SetId { set; get; }
        public VocabularySet Set { get; set; } = null!;
    }
}
