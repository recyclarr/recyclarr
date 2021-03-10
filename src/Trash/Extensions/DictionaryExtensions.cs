using System.Collections.Generic;

namespace Trash.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            if (!dict.TryGetValue(key, out var val))
            {
                val = new TValue();
                dict.Add(key, val);
            }

            return val;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            where TValue : struct
        {
            if (!dict.TryGetValue(key, out var val))
            {
                val = default;
                dict.Add(key, val);
            }

            return val;
        }
    }
}
