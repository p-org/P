using System.Collections.Generic;
using System.Linq;

namespace PSharpExtensions.Values
{
    internal static class HashHelper
    {
        private const uint HashSeed = 0x802CBBDB;

        public static int ComputeHash<T>(IEnumerable<T> values)
        {
            unchecked
            {
                return (int)values.Aggregate(HashSeed, (current, value) => current ^ (uint)value.GetHashCode());
            }
        }
    }
}