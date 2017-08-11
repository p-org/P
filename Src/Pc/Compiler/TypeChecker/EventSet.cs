using System.Collections.Immutable;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker
{
    public class EventSet : IPDeclaration
    {
        public EventSet(string name, IImmutableSet<PEvent> members, ParserRuleContext origin)
        {
            Name = name;
            Members = members;
            Origin = origin;
        }

        public IImmutableSet<PEvent> Members { get; }
        public string Name { get; }

        public ParserRuleContext Origin { get; }
    }
}