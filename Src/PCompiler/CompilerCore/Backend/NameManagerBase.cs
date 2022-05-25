using Plang.Compiler.TypeChecker.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Plang.Compiler.TypeChecker.AST.States;

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
            string name = baseName;
            while (nameUsages.TryGetValue(name, out int usages))
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

            if (TryGetNameForNode(decl, out string name))
            {
                return name;
            }

            string declName = ComputeNameForDecl(decl);
            return SetNameForNode(decl, declName);
        }

        /// <summary>
        /// Produces a Java String value representing a particular State.  This can be
        /// registered with State Builders and used as arguments to `gotoState()`.
        /// </summary>
        /// <param name="s">The state.</param>
        /// <returns>The identifier.</returns>
        public string IdentForState(State s)
        {
            return $"{s.Name.ToUpper()}_STATE";
        }
        
        protected abstract string ComputeNameForDecl(IPDecl decl);

        private string SetNameForNode(IPDecl node, string name)
        {
            if (declNames.TryGetValue(node, out string existing))
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