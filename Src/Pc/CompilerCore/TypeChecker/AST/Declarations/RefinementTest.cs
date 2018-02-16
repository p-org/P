using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class RefinementTest : IPDecl
    {
        public RefinementTest(ParserRuleContext sourceNode, string testName)
        {
            Name = testName;
            SourceLocation = sourceNode;
        }

        public IPModuleExpr LeftModExpr { get; set; }
        public IPModuleExpr RightModExpr { get; set; }
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
