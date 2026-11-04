namespace QuattroLingo.Entities
{
    public class Category
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public ICollection<Quiz> Quizzes { get; set; }
            = new List<Quiz>();
    }
}