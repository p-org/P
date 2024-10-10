using System.Collections.Generic;
using System.Linq;

namespace PChecker.Runtime.Values
{
    public class PEnum
    {
        private static readonly Dictionary<string, PInt> enumElements = new Dictionary<string, PInt>();

        public static PInt Get(string name)
        {
            return enumElements[name];
        }

        public static void AddEnumElements(string[] names, int[] values)
        {
            for (var i = 0; i < names.Length; i++)
            {
                enumElements.Add(names[i], values[i]);
            }
        }

        public static void Clear()
        {
            enumElements.Clear();
        }

        public object ToDict()
        {
            return enumElements.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDict());
        }
    }
}