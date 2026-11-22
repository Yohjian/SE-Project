using System.Collections;
using System.Text.Json;

namespace QuattroLingo.Models
{

    public class Metadata : IDictionary<string, MetadataEntry>
    {
        private readonly Dictionary<string, MetadataEntry> _inner = new();

        public void Set<T>(string key, T value) => _inner[key] = new MetadataEntry<T>(value);

        public T? Get<T>(string key)
        {
            if (!_inner.TryGetValue(key, out var entry)) return default;
            if (entry is MetadataEntry<T> typed) return typed.Value;
            // After a DB round-trip entries come back as JsonElement
            if (entry is MetadataEntry<JsonElement> json) return json.Value.Deserialize<T>();
            return default;
        }

        public T GetOrDefault<T>(string key, T defaultValue)
        {
            var result = Get<T>(key);
            return result is not null ? result : defaultValue;
        }

        // Used by EF Core converter
        internal Dictionary<string, object?> ToSerializable()
        {
            var dict = new Dictionary<string, object?>();
            foreach (var (key, entry) in _inner)
            {
                var type = entry.GetType();
                if (type.IsGenericType)
                    dict[key] = type.GetProperty("Value")?.GetValue(entry);
            }
            return dict;
        }

        internal static Metadata FromDictionary(Dictionary<string, JsonElement> dict)
        {
            var metadata = new Metadata();
            foreach (var (key, element) in dict)
                metadata[key] = new MetadataEntry<JsonElement>(element);
            return metadata;
        }

        // IDictionary
        public MetadataEntry this[string key] { get => _inner[key]; set => _inner[key] = value; }
        public ICollection<string> Keys => _inner.Keys;
        public ICollection<MetadataEntry> Values => _inner.Values;
        public int Count => _inner.Count;
        public bool IsReadOnly => false;

        public void Add(string key, MetadataEntry value) => _inner.Add(key, value);
        public bool ContainsKey(string key) => _inner.ContainsKey(key);
        public bool Remove(string key) => _inner.Remove(key);
        public bool TryGetValue(string key, out MetadataEntry value) => _inner.TryGetValue(key, out value!);

        public void Add(KeyValuePair<string, MetadataEntry> item) => ((IDictionary<string, MetadataEntry>)_inner).Add(item);
        public void Clear() => _inner.Clear();
        public bool Contains(KeyValuePair<string, MetadataEntry> item) => ((IDictionary<string, MetadataEntry>)_inner).Contains(item);
        public void CopyTo(KeyValuePair<string, MetadataEntry>[] array, int arrayIndex) => ((IDictionary<string, MetadataEntry>)_inner).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<string, MetadataEntry> item) => ((IDictionary<string, MetadataEntry>)_inner).Remove(item);

        public IEnumerator<KeyValuePair<string, MetadataEntry>> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
    }
}
