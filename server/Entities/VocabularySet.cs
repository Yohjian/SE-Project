namespace QuattroLingo.Entities
{
    public class VocabularySet
    {
        public int Id { set; get; }
        public string Name { set; get; } = string.Empty;
        public string UserId { set; get; } = string.Empty;
        public DateTime CreatedAt { set; get; }

        public int? FolderId { get; set; }
        public SetFolder? Folder { get; set; }

        public ICollection<VocabularyCard> Cards { get; set; } = [];
    }
}
