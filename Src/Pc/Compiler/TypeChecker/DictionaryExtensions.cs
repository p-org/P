using System;
using System.Collections.Generic;

namespace Microsoft.Pc.TypeChecker
{
    internal static class DictionaryExtensions
    {
        internal static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = valueFactory();
                dictionary.Add(key, value);
            }
            return value;
        }
    }
}