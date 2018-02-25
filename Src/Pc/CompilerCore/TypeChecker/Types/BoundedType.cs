using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class BoundedType : PLanguageType
    {
        private readonly Lazy<IReadOnlyList<PEvent>> allowedPermissions;

        public BoundedType(NamedEventSet eventSet) : base(TypeKind.Bounded)
        {
            EventSet = eventSet;
            allowedPermissions =
                new Lazy<IReadOnlyList<PEvent>>(() => (EventSet == null ? Enumerable.Empty<PEvent>() : EventSet.Events)
                                                    .ToList());
        }

        public NamedEventSet EventSet { get; }
        public override string OriginalRepresentation => EventSet == null ? "data" : $"any<{EventSet.Name}>";

        public override string CanonicalRepresentation =>
            EventSet == null ? "data" : $"any<{{{string.Join(",", EventSet.Events.Select(ev => ev.Name))}}}>";

        public override IReadOnlyList<PEvent> AllowedPermissions => allowedPermissions.Value;

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            throw new NotImplementedException("any<...> type checking");
        }

        public override PLanguageType Canonicalize()
        {
            return this;
        }
    }
}
