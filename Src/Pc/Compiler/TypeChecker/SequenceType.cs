namespace Microsoft.Pc.TypeChecker
{
    public class SequenceType : PLanguageType
    {
        public SequenceType(PLanguageType elementType) : base(TypeKind.Sequence)
        {
            ElementType = elementType;
        }

        public PLanguageType ElementType { get; set; }

        public override string OriginalRepresentation => $"seq[{ElementType.OriginalRepresentation}]";
        public override string CanonicalRepresentation => $"seq[{ElementType.CanonicalRepresentation}]";
    }
}