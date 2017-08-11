using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclarationTable
    {
        private readonly IDictionary<string, ParserRuleContext> allTrees = new Dictionary<string, ParserRuleContext>();

        private readonly IDictionary<Type, IDictionary<string, ParserRuleContext>> treesByType =
            new Dictionary<Type, IDictionary<string, ParserRuleContext>>();

        public DeclarationTable Parent { get; set; }

        public IImmutableDictionary<string, ParserRuleContext> AllSymbols => allTrees.ToImmutableDictionary();

        public bool Lookup<T>(string name, out T tree) where T : ParserRuleContext
        {
            return Get(name, out tree) || (Parent != null && Parent.Lookup(name, out tree));
        }

        public bool LookupAny(string name, out ParserRuleContext tree)
        {
            return GetAny(name, out tree) || (Parent != null && Parent.GetAny(name, out tree));
        }

        public bool Get<T>(string name, out T tree) where T : ParserRuleContext
        {
            if (!treesByType.TryGetValue(typeof(T), out var treeTable))
            {
                treeTable = new Dictionary<string, ParserRuleContext>();
                treesByType.Add(typeof(T), treeTable);
            }

            if (treeTable.TryGetValue(name, out var foundTree))
            {
                Debug.Assert(foundTree is T);
                tree = (T) foundTree;
                return true;
            }

            tree = null;
            return false;
        }

        public bool GetAny(string name, out ParserRuleContext tree)
        {
            return allTrees.TryGetValue(name, out tree);
        }

        public void Put<T>(string name, T tree) where T : ParserRuleContext
        {
            if (!treesByType.TryGetValue(typeof(T), out var treeTable))
            {
                treeTable = new Dictionary<string, ParserRuleContext>();
                treesByType.Add(typeof(T), treeTable);
            }

            treeTable.Add(name, tree);
            allTrees.Add(name, tree);
        }
    }
}