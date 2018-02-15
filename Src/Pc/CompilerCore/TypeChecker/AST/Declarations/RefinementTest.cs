using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class RefinementTest : IPDecl
    {
        private IPModuleExpr modExpr;
        public RefinementTest(ParserRuleContext sourceNode, string testName)
        {
            Name = testName;
            SourceLocation = sourceNode;
        }

        public IPModuleExpr ModExpr => modExpr;
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
