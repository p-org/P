using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class SafetyTest : IPDecl
    {
        private IPModuleExpr modExpr;
        public SafetyTest(ParserRuleContext sourceNode, string testName)
        {
            SourceLocation = sourceNode;
            Name = testName;
        }

        public IPModuleExpr ModExpr => modExpr;
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
