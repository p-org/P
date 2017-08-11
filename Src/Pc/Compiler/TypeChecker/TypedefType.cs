using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class TypedefType : PLanguageType, IPDeclaration
    {
        public TypedefType(string name, PParser.TypeDefDeclContext origin) : base(name, TypeKind.Typedef, name)
        {
            Name = name;
            Origin = origin;
        }

        public PLanguageType ActualType { get; set; }

        public ParserRuleContext Origin { get; }
        public string Name { get; }
    }
}