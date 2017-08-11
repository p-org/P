using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Pc.TypeChecker
{
    internal class NamedTupleType : PLanguageType
    {
        public NamedTupleType(string name, IReadOnlyList<TypedName> fields, string namedTupleRepr) : base(
            name,
            TypeKind.NamedTuple,
            namedTupleRepr)
        {
            Fields = fields;
        }

        public IEnumerable<PLanguageType> Types => Fields.Select(f => f.Type);
        public IEnumerable<string> Names => Fields.Select(f => f.Name);
        public IReadOnlyList<TypedName> Fields { get; }
    }
}