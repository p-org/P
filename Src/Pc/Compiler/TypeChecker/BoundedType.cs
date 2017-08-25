using System.Linq;

namespace Microsoft.Pc.TypeChecker
{
    public class BoundedType : PLanguageType
    {
        public BoundedType(EventSet eventSet) : base(TypeKind.Base)
        {
            EventSet = eventSet;
        }

        public EventSet EventSet { get; }
        public override string OriginalRepresentation => $"any<{EventSet.Name}>";
        public override string CanonicalRepresentation => $"any<{{{string.Join(",", EventSet.Events.Select(ev => ev.Name))}}}>";
        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            throw new System.NotImplementedException("any<> type checking");
        }

        public override PLanguageType Canonicalize() { return this; }
    }
}