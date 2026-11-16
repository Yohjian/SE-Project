namespace QuattroLingo.Entities
{
    public class Language
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public ICollection<Quiz> Quizzes { get; set; }
            = new List<Quiz>();
    }
}