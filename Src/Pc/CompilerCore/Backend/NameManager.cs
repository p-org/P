using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.Backend
{
    public class NameManager
    {
        private readonly string namePrefix;
        private readonly Dictionary<string, int> nameUsages = new Dictionary<string, int>();
        private readonly ConditionalWeakTable<IPDecl, string> declNames = new ConditionalWeakTable<IPDecl, string>();

        public NameManager(string namePrefix)
        {
            this.namePrefix = namePrefix;
        }

        public string GetNameForNode(IPDecl node)
        {
            if (declNames.TryGetValue(node, out string name))
            {
                return name;
            }

            name = node.Name;
            if (name.StartsWith("$"))
            {
                name = "PTMP_" + name.Substring(1);
            }
            else
            {
                name = namePrefix + name;
            }

            if (!nameUsages.TryGetValue(name, out int usages))
            {
                // name has not been used before
                nameUsages.Add(name, 1);
            }
            else
            {
                // name has been used `usages` times before
                nameUsages[name] = usages + 1;
                name = $"{name}_{usages}";
            }

            declNames.Add(node, name);
            return name;
        }
    }
}
