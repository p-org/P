using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class ProofBlock : IPDecl
    {
        public ProofBlock(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.ProofBlockContext);
            SourceLocation = sourceNode;
            Name = name;
        }
        public ParserRuleContext SourceLocation { get; }
        public List<ProofCommand> Commands { get; set; }
        public string Name { get; set; }
    }
}