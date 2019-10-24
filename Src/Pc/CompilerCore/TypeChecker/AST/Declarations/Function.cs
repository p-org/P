using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Statements;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    [Flags]
    public enum FunctionRole
    {
        Method = 1 << 1,
        EntryHandler = 1 << 2,
        TransitionFunction = 1 << 3,
        EventHandler = 1 << 4,
        ExitHandler = 1 << 5,
        ReceiveHandler = 1 << 6,
        Foreign = 1 << 7
    }

    public class Function : IPDecl, IHasScope
    {
        private readonly HashSet<Function> callees = new HashSet<Function>();
        private readonly HashSet<Function> callers = new HashSet<Function>();
        private readonly List<Variable> localVariables = new List<Variable>();
        private readonly List<Interface> createsInterfaces = new List<Interface>();

        public Function(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.FunDeclContext ||
                         sourceNode is PParser.AnonEventHandlerContext ||
                         sourceNode is PParser.NoParamAnonEventHandlerContext);
            Name = name;
            SourceLocation = sourceNode;
        }

        public Function(ParserRuleContext sourceNode) : this("", sourceNode)
        {
        }

        public Machine Owner { get; set; }
        public Function ParentFunction { get; set; }
        public FunctionSignature Signature { get; } = new FunctionSignature();
        public IEnumerable<Variable> LocalVariables => localVariables;
        public IEnumerable<Interface> CreatesInterfaces => createsInterfaces;
        public FunctionRole Role { get; set; }

        public CompoundStmt Body { get; set; }
        public Scope Scope { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }

        public void AddLocalVariable(Variable local)
        {
            localVariables.Add(local);
        }

        public void AddCreatesInterface(Interface i)
        {
            createsInterfaces.Add(i);
        }

        public void AddLocalVariables(IEnumerable<Variable> variables)
        {
            localVariables.AddRange(variables);
        }

        public void AddCallee(Function callee)
        {
            callee.callers.Add(this);
            callees.Add(callee);
        }

        #region Analysis results

        // TODO: decouple this? turn it into flags? a mix?
        public bool IsForeign => Body == null;

        public bool IsAnon => string.IsNullOrEmpty(Name);

        public bool? CanChangeState { get; set; }
        public bool? CanRaiseEvent { get; set; }
        public bool? CanReceive { get; set; }
        public bool? IsNondeterministic { get; set; }

        public IEnumerable<Function> Callers => callers;
        public IEnumerable<Function> Callees => callees;

        #endregion Analysis results
    }
}