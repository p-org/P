using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class RefinementTest : IPDecl
    {
        public RefinementTest(ParserRuleContext sourceNode, string testName)
        {
            Name = testName;
            SourceLocation = sourceNode;
        }

        public string Main { get; set; }
        public IPModuleExpr LeftModExpr { get; set; }
        public IPModuleExpr RightModExpr { get; set; }
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}