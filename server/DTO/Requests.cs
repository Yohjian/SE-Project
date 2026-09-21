namespace QuattroLingo.DTO
{
    public static class Requests
    {
        public record Register(string Email, string Password);
        public record Login(string Email, string Password);
    }
}
