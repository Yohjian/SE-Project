
namespace QuattroLingo.Models
{
    public class VocabularyCard()
    {
        public int ID { protected set; get; }
        public string Term { set; get; } = string.Empty;
        public string Definition { set; get; } = string.Empty;
        public int SetID { set; get; }
        public VocabularySet Set { get; set; } = null!;
    }
}
