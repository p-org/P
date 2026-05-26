using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class ProofCommand : IPDecl
    {
        public ProofCommand(string name, ParserRuleContext sourceNode)
        {
            // sourceNode is null for the synthetic "default"/"full" proof commands the
            // PVerifier backend generates when a project declares no proof blocks.
            Debug.Assert(sourceNode == null || sourceNode is PParser.ProofItemContext);
            SourceLocation = sourceNode;
            Name = name;
        }

        public List<Invariant> Goals { get; set; }
        public List<Invariant> Premises { get; set; }
        public List<Invariant> Excepts { get; set; }
        public ParserRuleContext SourceLocation { get; }
        public string Name { get; set; }
        public string ProofBlock { get; set; }
    }
}