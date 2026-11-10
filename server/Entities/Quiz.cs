namespace QuattroLingo.Entities
{
    public class Quiz
    {
        public int Id { get; set; }

        public required string OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public required string Title { get; set; }
        public string? Description { get; set; }

        public QuizKind Kind { get; set; } = QuizKind.Custom;

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public int? LanguageId { get; set; }
        public Language? Language { get; set; }

        public QuizDifficulty? Difficulty { get; set; }

        public bool IsPublic { get; set; } = false;
        public int? SourceQuizId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Question> Questions { get; set; }
            = new List<Question>();

        public int QuestionCount => Questions.Count;
    }
}
