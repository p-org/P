using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class Function : IHasScope
    {
        public Function(string name, PParser.FunDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public Function(PParser.AnonEventHandlerContext sourceNode)
        {
            Name = "";
            SourceNode = sourceNode;
        }

        public Function(PParser.NoParamAnonEventHandlerContext sourceNode)
        {
            Name = "";
            SourceNode = sourceNode;
        }

        public Machine Owner { get; set; }
        public FunctionSignature Signature { get; } = new FunctionSignature();
        public List<Variable> LocalVariables { get; } = new List<Variable>();
        public List<IPStmt> Body { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public DeclarationTable Table { get; set; }
    }
}
