namespace Plang.Compiler.TypeChecker.Types
{
    public class NamedTupleEntry
    {
        public string Name { get; set; }
        public int FieldNo { get; set; }
        public PLanguageType Type { get; set; }
    }
}