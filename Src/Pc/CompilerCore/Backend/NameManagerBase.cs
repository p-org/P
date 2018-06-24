using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.States;

namespace Microsoft.Pc.Backend
{
    public abstract class NameManagerBase
    {
        private readonly Dictionary<string, int> nameUsages = new Dictionary<string, int>();
        private readonly ConditionalWeakTable<IPDecl, string> declNames = new ConditionalWeakTable<IPDecl, string>();

        protected string NamePrefix { get; }

        protected NameManagerBase(string namePrefix)
        {
            this.NamePrefix = namePrefix;
        }

        public string GetTemporaryName(string baseName)
        {
            return AdjustName(NamePrefix + baseName);
        }

        protected string AdjustName(string baseName)
        {
            string name = baseName;
            while (nameUsages.TryGetValue(name, out int usages))
            {
                nameUsages[name] = usages + 1;
                name = $"{baseName}_{usages}";
            }

            nameUsages.Add(name, 1);
            return name;
        }

        public string GetNameForNode(IPDecl node, string prefix = "")
        {
            if (declNames.TryGetValue(node, out string name))
            {
                return name;
            }

            name = node.Name;
            if (node is State state)
            {
                name = state.QualifiedName;
            }
            name = string.IsNullOrEmpty(name) ? "Anon" : name;
            name = name.Replace('.', '_');

            if (name.StartsWith("$"))
            {
                name = "PTMP_" + name.Substring(1);
            }
            else
            {
                name = NamePrefix + prefix + name;
            }

            name = AdjustName(name);
            declNames.Add(node, name);
            return name;
        }
    }
}
