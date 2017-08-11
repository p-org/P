namespace Microsoft.Pc.TypeChecker
{
    public class SequenceType : PLanguageType
    {
        public SequenceType(string name, PLanguageType elementType) : base(
            name,
            TypeKind.Sequence,
            $"seq<{elementType.OriginalRepresentation}>")
        {
            ElementType = elementType;
        }

        public PLanguageType ElementType { get; set; }
    }
}