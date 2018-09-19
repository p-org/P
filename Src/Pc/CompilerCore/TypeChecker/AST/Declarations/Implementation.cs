using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Implementation : IPDecl
    {
        public Implementation(ParserRuleContext sourceNode, string name)
        {
            Name = name;
            SourceLocation = sourceNode;
        }

        public string Main { get; set; }
        public IPModuleExpr ModExpr { get; set; }
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
    }
}
