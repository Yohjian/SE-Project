public static class StringExtensions
{
    extension(string str)
    {
        public string SerializeEscapes() => str.Replace("\"", "\"\"");
    }
}
