using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class EventSet : IPDecl
    {
        public EventSet(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(string.Empty.Equals(name) && sourceNode == null ||
                         sourceNode is PParser.EventSetDeclContext ||
                         sourceNode is PParser.EventSetLiteralContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public SortedSet<PEvent> Events { get; } = new SortedSet<PEvent>(EventNameComparer);

        private static readonly Comparer<PEvent> EventNameComparer =
            Comparer<PEvent>.Create((ev1, ev2) => string.Compare(ev1.Name, ev2.Name, StringComparison.Ordinal));

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public IList<IPAST> Children => new List<IPAST>(Events);
    }
}
