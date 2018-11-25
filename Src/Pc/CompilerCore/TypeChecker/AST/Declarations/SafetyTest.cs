using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class SafetyTest : IPDecl
    {
        public SafetyTest(ParserRuleContext sourceNode, string testName)
        {
            SourceLocation = sourceNode;
            Name = testName;
        }

        public string Main { get; set; }
        public IPModuleExpr ModExpr { get; set; }
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}