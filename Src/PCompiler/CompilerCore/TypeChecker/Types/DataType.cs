using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public class DataType : PLanguageType
    {
        public DataType(NamedEventSet eventSet) : base(TypeKind.Data)
        {
        }

        public override string OriginalRepresentation => "data";

        public override string CanonicalRepresentation => "data";

        public override Lazy<IReadOnlyList<Event>> AllowedPermissions =>
            new Lazy<IReadOnlyList<Event>>(() => new List<Event>());

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            if (otherType.AllowedPermissions == null)
            {
                return false;
            }

            return !otherType.AllowedPermissions.Value.Any();
        }

        public override PLanguageType Canonicalize()
        {
            return this;
        }
    }
}