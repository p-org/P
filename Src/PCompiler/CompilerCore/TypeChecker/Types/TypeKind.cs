namespace Plang.Compiler.TypeChecker.Types
{
    public class TypeKind
    {
        public static readonly TypeKind Base = new TypeKind("base");
        public static readonly TypeKind Sequence = new TypeKind("sequence");
        public static readonly TypeKind Map = new TypeKind("map");
        public static readonly TypeKind Set = new TypeKind("set");
        public static readonly TypeKind Tuple = new TypeKind("tuple");
        public static readonly TypeKind NamedTuple = new TypeKind("namedtuple");
        public static readonly TypeKind Foreign = new TypeKind("foreign");
        public static readonly TypeKind Enum = new TypeKind("enum");
        public static readonly TypeKind TypeDef = new TypeKind("typedef");
        public static readonly TypeKind Data = new TypeKind("bounded");

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