using System.Collections.Concurrent;

namespace QuranBlazor.Services
{
    /// <summary>
    /// Simple LRU cache for transliteration results to improve performance
    /// </summary>
    public class TransliterationCache
    {
        private readonly ConcurrentDictionary<string, string> _cache = new();
        private readonly int _maxSize;
        private readonly object _lock = new object();

        public TransliterationCache(int maxSize = 1000)
        {
            _maxSize = maxSize;
        }

        public string GetOrAdd(string key, Func<string, string> valueFactory)
        {
            if (string.IsNullOrEmpty(key)) return key;

            if (_cache.TryGetValue(key, out var cachedValue))
            {
                return cachedValue;
            }

            var value = valueFactory(key);

            // Simple cache size management
            if (_cache.Count >= _maxSize)
            {
                lock (_lock)
                {
                    // Clear 20% of cache when full (simple eviction strategy)
                    if (_cache.Count >= _maxSize)
                    {
                        var itemsToRemove = _cache.Take(_maxSize / 5).Select(x => x.Key).ToList();
                        foreach (var item in itemsToRemove)
                        {
                            _cache.TryRemove(item, out _);
                        }
                    }
                }
            }

            _cache.TryAdd(key, value);
            return value;
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public int Count => _cache.Count;
    }
}
