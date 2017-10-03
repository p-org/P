using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class TypeDef : IPDecl
    {
        public TypeDef(string name, PParser.TypeDefDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public PLanguageType Type { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }
}
