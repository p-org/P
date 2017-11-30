using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Function : IPDecl, IHasScope
    {
        private readonly List<Variable> localVariables = new List<Variable>();

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
        public IEnumerable<Variable> LocalVariables => localVariables;

        public IPStmt Body { get; set; }
        public Scope Scope { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }

        public void AddLocalVariable(Variable local) { localVariables.Add(local); }
    }

    public enum FunctionRole
    {
        EventHandler,
        StaticFunction,
        ImplMachineMethod,
        SpecMachineMethod
    }
}
