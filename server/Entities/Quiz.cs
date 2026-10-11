namespace QuattroLingo.Entities
{
    public class Quiz
    {
        public int Id { get; set; }

        public required string OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public required string Title { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Question> Questions { get; set; }
            = new List<Question>();
    }
}