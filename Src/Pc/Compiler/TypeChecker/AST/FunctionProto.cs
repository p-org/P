using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class FunctionProto : IPDecl
    {
        public FunctionProto(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.ForeignFunDeclContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public FunctionSignature Signature { get; } = new FunctionSignature();
        public List<Machine> Creates { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public IList<IPAST> Children => throw new NotImplementedException("ast children");
    }
}
