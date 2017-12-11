using System;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class BoundedType : PLanguageType
    {
        public BoundedType(NamedEventSet eventSet) : base(TypeKind.Bounded) { EventSet = eventSet; }

        public NamedEventSet EventSet { get; }
        public override string OriginalRepresentation => EventSet == null ? "data" : $"any<{EventSet.Name}>";

        public override string CanonicalRepresentation =>
            EventSet == null ? "data" : $"any<{{{string.Join(",", EventSet.Events.Select(ev => ev.Name))}}}>";

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            throw new NotImplementedException("any<...> type checking");
        }

        public override PLanguageType Canonicalize() { return this; }
    }
}
