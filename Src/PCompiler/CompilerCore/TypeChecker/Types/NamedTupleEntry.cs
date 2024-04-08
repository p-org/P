namespace Plang.Compiler.TypeChecker.Types
{
    public class NamedTupleEntry
    {
        public NamedTupleEntry()
        {
        }

        public NamedTupleEntry(string name, int fieldNo, PLanguageType type)
        {
            Name = name;
            FieldNo = fieldNo;
            Type = type;
        }

        public string Name { get; set; }
        public int FieldNo { get; set; }
        public PLanguageType Type { get; set; }
    }
}