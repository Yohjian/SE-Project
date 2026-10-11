namespace QuattroLingo.Entities
{
    public class Question
    {
        public int Id { get; set; }

        public int QuizId { get; set; }
        public Quiz? Quiz { get; set; }

        public required string Text { get; set; }

        public int TimeLimitSeconds { get; set; }
        public int Points { get; set; }
        public int OrderIndex { get; set; }

        public ICollection<AnswerOption> AnswerOptions { get; set; }
            = new List<AnswerOption>();
    }
}