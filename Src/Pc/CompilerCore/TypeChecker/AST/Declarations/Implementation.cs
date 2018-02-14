using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Implementation : IPDecl
    {
        public Implementation(ParserRuleContext sourceNode)
        {
            Name = "implementation";
            SourceLocation = sourceNode;
        }

        Module module;

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
