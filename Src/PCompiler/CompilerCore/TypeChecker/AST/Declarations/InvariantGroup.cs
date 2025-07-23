using System.Collections.Generic;
using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class InvariantGroup : IPDecl
    {
        public InvariantGroup(string name, ParserRuleContext sourceNode)
        {
            Name = name;
            SourceLocation = sourceNode;
        }
        public List<Invariant> Invariants { get; set; }
        public ParserRuleContext SourceLocation { get; }
        public string Name { get; set; }
    }
}