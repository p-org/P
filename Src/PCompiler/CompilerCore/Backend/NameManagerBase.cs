using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Plang.Compiler.TypeChecker.AST;

namespace Plang.Compiler.Backend
{
    public abstract class NameManagerBase
    {
        private readonly ConditionalWeakTable<IPDecl, string> declNames = new ConditionalWeakTable<IPDecl, string>();
        private readonly Dictionary<string, int> nameUsages = new Dictionary<string, int>();

        protected NameManagerBase(string namePrefix)
        {
            NamePrefix = namePrefix;
        }

        protected string NamePrefix { get; }

        public string GetTemporaryName(string baseName)
        {
            return UniquifyName(NamePrefix + baseName);
        }

        protected string UniquifyName(string baseName)
        {
            var name = baseName;
            while (nameUsages.TryGetValue(name, out var usages))
            {
                nameUsages[name] = usages + 1;
                name = $"{baseName}_{usages}";
            }

            nameUsages.Add(name, 1);
            return name;
        }

        public string GetNameForDecl(IPDecl decl)
        {
            Contract.Requires(decl != null);

            if (TryGetNameForNode(decl, out var name))
            {
                return name;
            }

            var declName = ComputeNameForDecl(decl);
            return SetNameForNode(decl, declName);
        }

        protected abstract string ComputeNameForDecl(IPDecl decl);

        private string SetNameForNode(IPDecl node, string name)
        {
            if (declNames.TryGetValue(node, out var existing))
            {
                throw new ArgumentException($"Decl {node.Name} already has name {existing}", nameof(node));
            }

            declNames.Add(node, name);
            return name;
        }

        private bool TryGetNameForNode(IPDecl node, out string name)
        {
            return declNames.TryGetValue(node, out name);
        }
        
    }
}