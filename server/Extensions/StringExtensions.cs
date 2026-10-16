namespace QuattroLingo.Extensions;

public static class StringExtensions
{
    extension(string value)
    {
        public string ToCsvField() => $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
