using System.Diagnostics;
using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class EnumElem : IPDecl
    {
        public EnumElem(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.EnumElemContext || sourceNode is PParser.NumberedEnumElemContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public int Value { get; set; }
        public PEnum ParentEnum { get; set; }

        public ParserRuleContext SourceLocation { get; }
        public string Name { get; }
    }
}