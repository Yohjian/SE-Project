namespace QuattroLingo.Entity
{
    public class Language
    {
        public int Id { get; set; }
        public required string Code { get; set; }   // e.g. "en", "lt"
        public required string Name { get; set; }   // e.g. "English"
    }
}