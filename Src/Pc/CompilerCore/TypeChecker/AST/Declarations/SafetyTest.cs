using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class SafetyTest : IPDecl
    {
        public SafetyTest(string testName, ParserRuleContext sourceNode)
        {
            
        }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
