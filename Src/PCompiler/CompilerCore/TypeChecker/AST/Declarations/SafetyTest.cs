using Antlr4.Runtime;
using System.Collections.Generic;

public enum TestKind {
    NormalTest,
    ParametricTest,
    AssumeParametricTest,
}

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
        
        public IDictionary<string, List<IPExpr>> ParamExpr { get; set; }
        public IPExpr AssumeExpr { get; set; }
        public TestKind TestKind { get; set; }
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}