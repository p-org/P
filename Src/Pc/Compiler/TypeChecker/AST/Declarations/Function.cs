using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Function : IPDecl, IHasScope
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
        public CompoundStmt Body { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public IList<IPAST> Children => throw new NotImplementedException("ast children");
        public IPAST Parent => throw new NotImplementedException();
        public Scope Table { get; set; }
    }
}
