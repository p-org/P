using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class FunctionProto : IPDecl
    {
        public FunctionProto(string name, PParser.FunProtoDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public FunctionSignature Signature { get; } = new FunctionSignature();
        public List<Machine> Creates { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }
}
