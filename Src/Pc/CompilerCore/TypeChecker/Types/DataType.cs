using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class DataType : PLanguageType
    {

        public DataType(NamedEventSet eventSet) : base(TypeKind.Bounded)
        {
        }

        public override string OriginalRepresentation => "data";

        public override string CanonicalRepresentation => "data";

        public override IReadOnlyList<PEvent> AllowedPermissions => new List<PEvent>();

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return !otherType.AllowedPermissions.Any();
        }

        public override PLanguageType Canonicalize()
        {
            return this;
        }
    }
}
