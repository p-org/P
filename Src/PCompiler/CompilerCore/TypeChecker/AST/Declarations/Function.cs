using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;

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
        Foreign = 1 << 7,
        Predicate = 1 << 8,
        Function = 1 << 9
    }

    public enum FunctionProperty
    {
        None = 0,
        Reflexive = 1 << 1,
        Symmetric = 1 << 2,
        Transitive = 1 << 3,
        AntiSymmetric = 1 << 4,
        Idempotent = 1 << 5,
        AntiReflexive = 1 << 6,
        Asymmetric = 1 << 7
    }

    public class Function : IPDecl, IHasScope
    {
        private readonly HashSet<Function> callees = new HashSet<Function>();
        private readonly HashSet<Function> callers = new HashSet<Function>();
        private readonly List<Variable> localVariables = new List<Variable>();
        private readonly List<Interface> createsInterfaces = new List<Interface>();
        private readonly HashSet<PEvent> sendsSet = [];
        private readonly HashSet<PEvent> recvSet = [];
        private readonly HashSet<State> gotoStates = [];
        private readonly List<IPExpr> equivalences = [];
        private readonly List<IPExpr> contradictions = [];
        private readonly List<IPExpr> negation = [];
        private readonly List<IPExpr> impliedBy = [];

        public Function(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.FunDeclContext ||
                         sourceNode is PParser.AnonEventHandlerContext ||
                         sourceNode is PParser.NoParamAnonEventHandlerContext ||
                         sourceNode is PParser.ReceiveStmtContext ||
                         sourceNode is PParser.WhileStmtContext ||
                         sourceNode is PParser.ForeachStmtContext);
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
        public FunctionProperty Property { get; set; }
        public IEnumerable<IPExpr> Equivalences => equivalences;
        public IEnumerable<IPExpr> Contradictions => contradictions;
        public IEnumerable<IPExpr> Negation => negation;
        public IEnumerable<IPExpr> ImpliedBy => impliedBy;

        public CompoundStmt Body { get; set; }
        public Scope Scope { get; set; }

        public string Name { get; set; }
        public ParserRuleContext SourceLocation { get; }

        public void AddEquiv(IPExpr e)
        {
            equivalences.Add(e);
        }

        public void AddContradiction(IPExpr e)
        {
            contradictions.Add(e);
        }

        public void AddNegation(IPExpr e)
        {
            negation.Add(e);
        }

        public void AddImpliedBy(IPExpr e)
        {
            impliedBy.Add(e);
        }

        public void AddLocalVariable(Variable local)
        {
            localVariables.Add(local);
        }

        public void RemoveLocalVariable(Variable local)
        {
            localVariables.Remove(local);
        }

        public void AddSends(PEvent e)
        {
            sendsSet.Add(e);
            // Propagates to callers
            foreach (var caller in Callers)
            {
                if (caller.CanSend != true) caller.CanSend = true;
                caller.AddSends(e);
            }
            ParentFunction?.AddSends(e);
        }

        public void AddRecv(PEvent e)
        {
            recvSet.Add(e);
            foreach (var caller in Callers)
            {
                caller.AddRecv(e);
            }
            ParentFunction?.AddRecv(e);
        }

        public void AddGoto(State s)
        {
            gotoStates.Add(s);
            foreach (var caller in Callers)
            {
                caller.AddGoto(s);
            }
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

        public void RemoveCallee(Function callee)
        {
            callee.callers.Remove(this);
            callees.Remove(callee);
        }

        #region Analysis results

        // TODO: decouple this? turn it into flags? a mix?
        public bool IsForeign => Body == null;

        public bool IsAnon => string.IsNullOrEmpty(Name);

        public bool? CanChangeState { get; set; }
        public bool? CanRaiseEvent { get; set; }
        public bool? CanReceive { get; set; }

        public bool? CanSend { get; set; }
        public IEnumerable<PEvent> SendSet => sendsSet;
        public IEnumerable<PEvent> RecvSet => recvSet;
        public IEnumerable<State> NextStates => gotoStates;

        public bool? CanCreate { get; set; }
        public bool? IsNondeterministic { get; set; }

        public IEnumerable<Function> Callers => callers;
        public IEnumerable<Function> Callees => callees;

        #endregion Analysis results
    }
}