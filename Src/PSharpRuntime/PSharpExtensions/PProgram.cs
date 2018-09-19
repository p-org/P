using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.PSharp;

namespace PSharpExtensions
{
    public partial class PProgram
    {
        public static Dictionary<string, List<string>> interfaces;
        public static Dictionary<string, string> interfaceDefinitionMap;
        public static Dictionary<string, IEnumerable<string>> monitorMap;
        public static IDictionary<string, IDictionary<string, string>> linkMap = new Dictionary<string, IDictionary<string, string>>();

        

        public static void AddInterface(string interfaceName, params string[] permissions)
        {
            interfaces.Add(interfaceName, permissions.ToList());
        }

    }
}
