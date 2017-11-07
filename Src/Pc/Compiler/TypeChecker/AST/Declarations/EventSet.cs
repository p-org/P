using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class EventSet : IPDecl
    {
        private static readonly Comparer<PEvent> EventNameComparer =
            Comparer<PEvent>.Create((ev1, ev2) => string.Compare(ev1.Name, ev2.Name, StringComparison.Ordinal));

        private readonly SortedSet<PEvent> events = new SortedSet<PEvent>(EventNameComparer);

        public EventSet(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(string.Empty.Equals(name) && sourceNode == null ||
                         sourceNode is PParser.EventSetDeclContext ||
                         sourceNode is PParser.EventSetLiteralContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public IEnumerable<PEvent> Events => events;

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public void AddEvent(PEvent evt) { events.Add(evt); }
        public void AddEvents(IEnumerable<PEvent> evts) { events.UnionWith(evts); }
    }
}
