using System.Collections.Generic;
using System.Linq;

namespace PSharpExtensions
{
    public class PInterfaces
    {
        private static readonly Dictionary<string, List<string>> Interfaces = new Dictionary<string, List<string>>();

        public static void AddInterface(string interfaceName, params string[] permissions)
        {
            Interfaces.Add(interfaceName, permissions.ToList());
        }

        public static List<string> GetPermissions(string interfaceName)
        {
            return Interfaces[interfaceName].ToList();
        }
    }
}
