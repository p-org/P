using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;
using System.Diagnostics;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class TypeDef : IPDecl
    {
        public TypeDef(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.TypeDefDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public PLanguageType Type { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}