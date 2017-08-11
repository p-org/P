using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker
{
    public class PEvent : IPDeclaration
    {
        public PEvent(ParserRuleContext origin, string name)
        {
            Origin = origin;
            Name = name;
        }

        public int Assert { get; set; }
        public int Assume { get; set; }
        public PLanguageType PayloadType { get; set; }

        public ParserRuleContext Origin { get; }
        public string Name { get; }
    }
}