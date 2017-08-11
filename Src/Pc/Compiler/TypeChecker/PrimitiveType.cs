namespace Microsoft.Pc.TypeChecker
{
    public class PrimitiveType : PLanguageType
    {
        public static readonly PrimitiveType Bool = new PrimitiveType("bool", "bool");
        public static readonly PrimitiveType Int = new PrimitiveType("int", "int");
        public static readonly PrimitiveType Float = new PrimitiveType("float", "float");
        public static readonly PrimitiveType Event = new PrimitiveType("event", "event");
        public static readonly PrimitiveType Machine = new PrimitiveType("machine", "machine");
        public static readonly PrimitiveType Data = new PrimitiveType("data", "data");
        public static readonly PrimitiveType Any = new PrimitiveType("any", "any");
        public static readonly PrimitiveType Null = new PrimitiveType("null", "null");

        private PrimitiveType(string name, string repr) : base(name, TypeKind.Base, repr) { }
    }
}