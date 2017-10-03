using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class EventSet : IPDecl
    {
        public EventSet(string name, PParser.EventSetDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public EventSet(string name, PParser.EventSetLiteralContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public SortedSet<PEvent> Events { get; } =
            new SortedSet<PEvent>(
                                  Comparer<PEvent>.Create(
                                                          (ev1, ev2) => string.Compare(
                                                                                       ev1.Name,
                                                                                       ev2.Name,
                                                                                       StringComparison.Ordinal)));

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }
}
