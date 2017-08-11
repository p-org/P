namespace Microsoft.Pc.TypeChecker
{
    public class TypeKind
    {
        public static readonly TypeKind Base = new TypeKind("base");
        public static readonly TypeKind Sequence = new TypeKind("sequence");
        public static readonly TypeKind Map = new TypeKind("map");
        public static readonly TypeKind Tuple = new TypeKind("tuple");
        public static readonly TypeKind NamedTuple = new TypeKind("namedtuple");
        public static readonly TypeKind Foreign = new TypeKind("foreign");

        public static readonly TypeKind Typedef = new TypeKind("typedef");
        public static readonly TypeKind Machine = new TypeKind("machine");

        private TypeKind(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}