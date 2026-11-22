namespace QuattroLingo.Models
{
    public abstract class MetadataEntry
    {
    }

    public sealed class MetadataEntry<T> : MetadataEntry
    {
        public T Value { get; }

        public MetadataEntry(T value) => Value = value;

        public MetadataEntry<TResult> Transform<TResult>(Func<T, TResult> transform)
            => new(transform(Value));
    }
}
