using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class Function : IHasScope
    {
        public Function(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.FunDeclContext ||
                         sourceNode is PParser.AnonEventHandlerContext ||
                         sourceNode is PParser.NoParamAnonEventHandlerContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public Function(ParserRuleContext sourceNode) : this("", sourceNode) { }

        public Machine Owner { get; set; }
        public FunctionSignature Signature { get; } = new FunctionSignature();
        public List<Variable> LocalVariables { get; } = new List<Variable>();
        public List<IPStmt> Body { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public IList<IPAST> Children => throw new NotImplementedException("ast children");
        public Scope Table { get; set; }
    }
}
