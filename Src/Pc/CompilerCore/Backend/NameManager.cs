using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.Backend
{
    public class NameManager
    {
        private readonly ConditionalWeakTable<IPDecl, string> settledNames = new ConditionalWeakTable<IPDecl, string>();
        private readonly HashSet<string> allNames = new HashSet<string>();

        public string GetNameForNode(IPDecl node)
        {
            if (settledNames.TryGetValue(node, out string name))
            {
                return name;
            }

            name = node.Name;
            if (name.StartsWith("$"))
            {
                name = "tmp_" + name.Substring(1);
            }
            else
            {
                name = "pSrc_" + name;
            }

            name = AdjustName(name);
            allNames.Add(name);
            settledNames.Add(node, name);
            return name;
        }

        private string AdjustName(string name)
        {
            // This takes O(|allNames|). It could be made more efficient by
            // using a trie to walk down to the desired name, 
            var suffix = 2;
            string baseName = name;
            while (allNames.Contains(name))
            {
                name = $"{baseName}_{suffix}";
                suffix++;
            }
            return name;
        }
    }
}