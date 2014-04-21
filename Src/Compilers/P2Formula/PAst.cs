using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QUT.Gppg;

namespace PParser
{
    public abstract class BasePVisitor<T>
    {
        protected Dictionary<INode, T> pre_result, post_result;
        private INode parent;
        
        public BasePVisitor() {
            pre_result = new Dictionary<INode, T>();
            post_result = new Dictionary<INode, T>();
            parent = null;
        }

        // Type Declarations
        public abstract T visit(TypeInt s);
        public abstract T visit(TypeBool s);
        public abstract T visit(TypeMachineID s);
        public abstract T visit(TypeModelMachineID s);
        public abstract T visit(TypeEventID s);
        public abstract T visit(TypeNamedTuple s);
        public abstract T visit(TypeField s);
        public abstract T visit(TypeTuple s);
        public abstract T visit(TypeSeq s);
        public abstract T visit(TypeMap s);
        public abstract T visit(TypeAny s);
        // P Declarations
        public abstract T visit(Program s);
        public abstract T visit(EventDeclaration s);
        public abstract T visit(MachineDeclaration s);
        public abstract T visit(MonitorDeclaration s);
        // Machine Declarations
        public abstract T visit(VarDeclaration s);
        public abstract T visit(FunDeclaration s);
        public abstract T visit(StateDeclaration s);
        public abstract T visit(SubmachineDeclaration s);
        public abstract T visit(ActionDeclaration s);
        public abstract T visit(MaxQueueDeclaration s);
        // State Declarations
        public abstract T visit(EntryFunction s);
        public abstract T visit(ExitFunction s);
        public abstract T visit(Defer s);
        public abstract T visit(Ignore s);
        public abstract T visit(Transition s);
        public abstract T visit(CallTransition s);
        public abstract T visit(Action s);
        // DSL Statements
        public abstract T visit(DSLITE s) ;
        public abstract T visit(DSLWhile s) ;
        public abstract T visit(DSLAssign s) ;
        public abstract T visit(DSLBlock s) ;
        public abstract T visit(DSLFFCallStmt s) ;
        public abstract T visit(DSLSend s) ;
        public abstract T visit(DSLSCall s) ;
        public abstract T visit(DSLMCall s);
        public abstract T visit(DSLRaise s) ;
        public abstract T visit(DSLAssert s) ;
        public abstract T visit(DSLReturn s) ;
        public abstract T visit(DSLLeave s) ;
        public abstract T visit(DSLSkip s) ;
        public abstract T visit(DSLMutation s) ;
        public abstract T visit(DSLNewStmt s) ;
        // DSL Expressions
        public abstract T visit(DSLId e) ;
        public abstract T visit(DSLMember e);
        public abstract T visit(DSLIndex e);
        public abstract T visit(DSLArg e);
        public abstract T visit(DSLInt e) ;
        public abstract T visit(DSLBool e) ;
        public abstract T visit(DSLFFCall e) ;
        public abstract T visit(DSLUnop e) ;
        public abstract T visit(DSLBinop e) ;
        public abstract T visit(DSLTuple e) ;
        public abstract T visit(DSLNamedTuple e);
        public abstract T visit(DSLKWArgs e) ;
        public abstract T visit(DSLNew e) ;
        public abstract T visit(DSLAttribute e);
        public abstract T visit(DSLSizeof e);
        public abstract T visit(DSLKeys e);

        // Type Declarations
        public abstract T visit_pre(TypeInt s);
        public abstract T visit_pre(TypeBool s);
        public abstract T visit_pre(TypeMachineID s);
        public abstract T visit_pre(TypeModelMachineID s);
        public abstract T visit_pre(TypeEventID s);
        public abstract T visit_pre(TypeField s);
        public abstract T visit_pre(TypeNamedTuple s);
        public abstract T visit_pre(TypeTuple s);
        public abstract T visit_pre(TypeSeq s);
        public abstract T visit_pre(TypeMap s);
        public abstract T visit_pre(TypeAny s);
        // P Declarations
        public abstract T visit_pre(Program s);
        public abstract T visit_pre(EventDeclaration s);
        public abstract T visit_pre(MachineDeclaration s);
        public abstract T visit_pre(MonitorDeclaration s);
        // Machine Declarations
        public abstract T visit_pre(VarDeclaration s);
        public abstract T visit_pre(FunDeclaration s);
        public abstract T visit_pre(StateDeclaration s);
        public abstract T visit_pre(SubmachineDeclaration s);
        public abstract T visit_pre(ActionDeclaration s);
        public abstract T visit_pre(MaxQueueDeclaration s);
        // State Declarations
        public abstract T visit_pre(EntryFunction s);
        public abstract T visit_pre(ExitFunction s);
        public abstract T visit_pre(Defer s);
        public abstract T visit_pre(Ignore s);
        public abstract T visit_pre(Transition s);
        public abstract T visit_pre(CallTransition s);
        public abstract T visit_pre(Action s);
        // DSL Statements
        public abstract T visit_pre(DSLITE s);
        public abstract T visit_pre(DSLWhile s);
        public abstract T visit_pre(DSLAssign s);
        public abstract T visit_pre(DSLBlock s);
        public abstract T visit_pre(DSLFFCallStmt s);
        public abstract T visit_pre(DSLSend s);
        public abstract T visit_pre(DSLSCall s);
        public abstract T visit_pre(DSLMCall s);
        public abstract T visit_pre(DSLRaise s);
        public abstract T visit_pre(DSLAssert s);
        public abstract T visit_pre(DSLReturn s);
        public abstract T visit_pre(DSLLeave s);
        public abstract T visit_pre(DSLSkip s);
        public abstract T visit_pre(DSLMutation s);
        public abstract T visit_pre(DSLNewStmt s);
        // DSL Expressions
        public abstract T visit_pre(DSLId e);
        public abstract T visit_pre(DSLMember e);
        public abstract T visit_pre(DSLIndex e);
        public abstract T visit_pre(DSLArg e);
        public abstract T visit_pre(DSLInt e);
        public abstract T visit_pre(DSLBool e);
        public abstract T visit_pre(DSLFFCall e);
        public abstract T visit_pre(DSLUnop e);
        public abstract T visit_pre(DSLBinop e);
        public abstract T visit_pre(DSLTuple e);
        public abstract T visit_pre(DSLNamedTuple e);
        public abstract T visit_pre(DSLKWArgs e);
        public abstract T visit_pre(DSLNew e);
        public abstract T visit_pre(DSLAttribute e);
        public abstract T visit_pre(DSLSizeof e);
        public abstract T visit_pre(DSLKeys e);

        public T visit(INode n)
        {
            if (n is TypeInt) { return this.visit(n as TypeInt); }
            if (n is TypeBool) { return this.visit(n as TypeBool); }
            if (n is TypeMachineID) { return this.visit(n as TypeMachineID); }
            if (n is TypeModelMachineID) { return this.visit(n as TypeModelMachineID); }
            if (n is TypeEventID) { return this.visit(n as TypeEventID); }
            if (n is TypeNamedTuple) { return this.visit(n as TypeNamedTuple); }
            if (n is TypeField) { return this.visit(n as TypeField); }
            if (n is TypeTuple) { return this.visit(n as TypeTuple); }
            if (n is TypeSeq) { return this.visit(n as TypeSeq); }
            if (n is TypeMap) { return this.visit(n as TypeMap); }
            if (n is TypeAny) { return this.visit(n as TypeAny); }

            if (n is Program) { return this.visit(n as Program); }
            if (n is EventDeclaration) { return this.visit(n as EventDeclaration); }
            if (n is MachineDeclaration) { return this.visit(n as MachineDeclaration); }
            if (n is MonitorDeclaration) { return this.visit(n as MonitorDeclaration); }

            if (n is VarDeclaration) { return this.visit(n as VarDeclaration); }
            if (n is StateDeclaration) { return this.visit(n as StateDeclaration); }
            if (n is SubmachineDeclaration) { return this.visit(n as SubmachineDeclaration); }
            if (n is FunDeclaration) { return this.visit(n as FunDeclaration); }
            if (n is ActionDeclaration) { return this.visit(n as ActionDeclaration); }
            if (n is MaxQueueDeclaration) { return this.visit(n as MaxQueueDeclaration); }

            if (n is EntryFunction) { return this.visit(n as EntryFunction); }
            if (n is ExitFunction) { return this.visit(n as ExitFunction); }
            if (n is Defer) { return this.visit(n as Defer); }
            if (n is Ignore) { return this.visit(n as Ignore); }
            if (n is Transition) { return this.visit(n as Transition); }
            if (n is CallTransition) { return this.visit(n as CallTransition); }
            if (n is Action) { return this.visit(n as Action); }

            if (n is DSLITE) { return this.visit(n as DSLITE); }
            if (n is DSLWhile) { return this.visit(n as DSLWhile); }
            if (n is DSLAssign) { return this.visit(n as DSLAssign); }
            if (n is DSLBlock) { return this.visit(n as DSLBlock); }
            if (n is DSLFFCallStmt) { return this.visit(n as DSLFFCallStmt); }
            if (n is DSLSend) { return this.visit(n as DSLSend); }
            if (n is DSLSCall) { return this.visit(n as DSLSCall); }
            if (n is DSLMCall) { return this.visit(n as DSLMCall); }
            if (n is DSLRaise) { return this.visit(n as DSLRaise); }
            if (n is DSLAssert) { return this.visit(n as DSLAssert); }
            if (n is DSLReturn) { return this.visit(n as DSLReturn); }
            if (n is DSLLeave) { return this.visit(n as DSLLeave); }
            if (n is DSLSkip) { return this.visit(n as DSLSkip); }
            if (n is DSLMutation) { return this.visit(n as DSLMutation); }
            if (n is DSLNewStmt) { return this.visit(n as DSLNewStmt); }

            if (n is DSLId) { return this.visit(n as DSLId); }
            if (n is DSLMember) { return this.visit(n as DSLMember); }
            if (n is DSLIndex) { return this.visit(n as DSLIndex); }
            if (n is DSLArg) { return this.visit(n as DSLArg); }
            if (n is DSLInt) { return this.visit(n as DSLInt); }
            if (n is DSLBool) { return this.visit(n as DSLBool); }
            if (n is DSLFFCall) { return this.visit(n as DSLFFCall); }
            if (n is DSLUnop) { return this.visit(n as DSLUnop); }
            if (n is DSLBinop) { return this.visit(n as DSLBinop); }
            if (n is DSLTuple) { return this.visit(n as DSLTuple); }
            if (n is DSLNamedTuple) { return this.visit(n as DSLNamedTuple); }
            if (n is DSLKWArgs) { return this.visit(n as DSLKWArgs); }
            if (n is DSLNew) { return this.visit(n as DSLNew); }
            if (n is DSLAttribute) { return this.visit(n as DSLAttribute); }
            if (n is DSLSizeof) { return this.visit(n as DSLSizeof); }
            if (n is DSLKeys) { return this.visit(n as DSLKeys); }

            throw new NotImplementedException("Unknown node type " + n.GetType().FullName);
        }

        public T visit_pre(INode n)
        {
            if (n is TypeInt) { return this.visit_pre(n as TypeInt); }
            if (n is TypeBool) { return this.visit_pre(n as TypeBool); }
            if (n is TypeMachineID) { return this.visit_pre(n as TypeMachineID); }
            if (n is TypeModelMachineID) { return this.visit_pre(n as TypeModelMachineID); }
            if (n is TypeEventID) { return this.visit_pre(n as TypeEventID); }
            if (n is TypeNamedTuple) { return this.visit_pre(n as TypeNamedTuple); }
            if (n is TypeField) { return this.visit_pre(n as TypeField); }
            if (n is TypeTuple) { return this.visit_pre(n as TypeTuple); }
            if (n is TypeSeq) { return this.visit_pre(n as TypeSeq); }
            if (n is TypeMap) { return this.visit_pre(n as TypeMap); }
            if (n is TypeAny) { return this.visit_pre(n as TypeAny); }

            if (n is Program) { return this.visit_pre(n as Program); }
            if (n is EventDeclaration) { return this.visit_pre(n as EventDeclaration); }
            if (n is MachineDeclaration) { return this.visit_pre(n as MachineDeclaration); }
            if (n is MonitorDeclaration) { return this.visit_pre(n as MonitorDeclaration); }
            if (n is MaxQueueDeclaration) { return this.visit_pre(n as MaxQueueDeclaration); }

            if (n is VarDeclaration) { return this.visit_pre(n as VarDeclaration); }
            if (n is StateDeclaration) { return this.visit_pre(n as StateDeclaration); }
            if (n is SubmachineDeclaration) { return this.visit_pre(n as SubmachineDeclaration); }
            if (n is ActionDeclaration) { return this.visit_pre(n as ActionDeclaration); }
            if (n is FunDeclaration) { return this.visit_pre(n as FunDeclaration); }

            if (n is EntryFunction) { return this.visit_pre(n as EntryFunction); }
            if (n is ExitFunction) { return this.visit_pre(n as ExitFunction); }
            if (n is Defer) { return this.visit_pre(n as Defer); }
            if (n is Ignore) { return this.visit_pre(n as Ignore); }
            if (n is Transition) { return this.visit_pre(n as Transition); }
            if (n is CallTransition) { return this.visit_pre(n as CallTransition); }
            if (n is Action) { return this.visit_pre(n as Action); }

            if (n is DSLITE) { return this.visit_pre(n as DSLITE); }
            if (n is DSLWhile) { return this.visit_pre(n as DSLWhile); }
            if (n is DSLAssign) { return this.visit_pre(n as DSLAssign); }
            if (n is DSLBlock) { return this.visit_pre(n as DSLBlock); }
            if (n is DSLFFCallStmt) { return this.visit_pre(n as DSLFFCallStmt); }
            if (n is DSLSend) { return this.visit_pre(n as DSLSend); }
            if (n is DSLSCall) { return this.visit_pre(n as DSLSCall); }
            if (n is DSLMCall) { return this.visit_pre(n as DSLMCall); }
            if (n is DSLRaise) { return this.visit_pre(n as DSLRaise); }
            if (n is DSLAssert) { return this.visit_pre(n as DSLAssert); }
            if (n is DSLReturn) { return this.visit_pre(n as DSLReturn); }
            if (n is DSLLeave) { return this.visit_pre(n as DSLLeave); }
            if (n is DSLSkip) { return this.visit_pre(n as DSLSkip); }
            if (n is DSLMutation) { return this.visit_pre(n as DSLMutation); }
            if (n is DSLNewStmt) { return this.visit_pre(n as DSLNewStmt); }

            if (n is DSLId) { return this.visit_pre(n as DSLId); }
            if (n is DSLMember) { return this.visit_pre(n as DSLMember); }
            if (n is DSLIndex) { return this.visit_pre(n as DSLIndex); }
            if (n is DSLArg) { return this.visit_pre(n as DSLArg); }
            if (n is DSLInt) { return this.visit_pre(n as DSLInt); }
            if (n is DSLBool) { return this.visit_pre(n as DSLBool); }
            if (n is DSLFFCall) { return this.visit_pre(n as DSLFFCall); }
            if (n is DSLUnop) { return this.visit_pre(n as DSLUnop); }
            if (n is DSLBinop) { return this.visit_pre(n as DSLBinop); }
            if (n is DSLTuple) { return this.visit_pre(n as DSLTuple); }
            if (n is DSLNamedTuple) { return this.visit_pre(n as DSLNamedTuple); }
            if (n is DSLKWArgs) { return this.visit_pre(n as DSLKWArgs); }
            if (n is DSLNew) { return this.visit_pre(n as DSLNew); }
            if (n is DSLAttribute) { return this.visit_pre(n as DSLAttribute); }
            if (n is DSLSizeof) { return this.visit_pre(n as DSLSizeof); }
            if (n is DSLKeys) { return this.visit_pre(n as DSLKeys); }

            throw new NotImplementedException("Unknown node type " + n.GetType().FullName);
        }


        public virtual void walk(INode n)
        {
            pre_result[n] = visit_pre(n);

            INode oldParent = parent;
            parent = n;

            foreach (INode child in n.children)
                walk(child);

            parent = oldParent;

            post_result[n] = visit(n);
        }

        protected INode getParent() { return parent; }

        public T getPreResult(INode n) { return pre_result[n]; }
        public T getPostResult(INode n) { return post_result[n]; }
    }

    public class DSLLoc
    {
        LexLocation l;
        internal DSLLoc(LexLocation l) { this.l = l; }

        internal DSLLoc(Microsoft.Formula.API.Span span)
        {
            l = new LexLocation(span.StartLine, span.StartCol, span.EndLine, span.EndCol);
        }

        public int startLine {
            get {
                if (l != null)
                    return l.StartLine;
                else
                    return 0;
            }
        }

        public int startColumn {
            get {
                if (l != null)
                    return l.StartColumn;
                else
                    return 0;
            }
        }

        public int endLine
        {
            get
            {
                if (l != null)
                    return l.EndLine;
                else
                    return 0;
            }
        }

        public int endColumn
        {
            get
            {
                if (l != null)
                    return l.EndColumn;
                else
                    return 0;
            }
        }

        public string str()
        {
            if (l != null)
                return "line " + l.StartLine + " col " + l.StartColumn;
            else
                return "";
        }
    }

    // AST Nodes
    public interface INode
    {
        IEnumerable<INode> children { get; }
        DSLLoc loc { get; set; }
    }

    // All AST Nodes subclass from this class
    public abstract class BaseNode : INode
    {
        private DSLLoc location;
        protected List<INode> child_lst;

        public BaseNode()
        {
            child_lst = new List<INode>();
        }

        public BaseNode(params INode[] cs)
        {
            child_lst = new List<INode>();
            foreach (INode n in cs)
            {
                if (n != null)
                    child_lst.Add(n);
            }
        }

        public DSLLoc loc
        {
            get { return location; }
            set { location = value; }
        }


        public virtual IEnumerable<INode> children
        {
            get
            {
                foreach (INode child in child_lst)
                    yield return child;
            }
        }

        public int count
        {
            get { return child_lst.Count; }
        }

        public void _add(INode el) { child_lst.Add(el); }
        public void _prepend(INode el) { child_lst.Insert(0, el); }

    }

    // All AST Nodes subclass from this class
    public abstract class BaseListNode<T> : BaseNode where T : INode
    {
        public BaseListNode() : base() { }
        public BaseListNode(IEnumerable<T> l) : base()
        {
            foreach (T c in l) child_lst.Add(c);
        }

        public void add(T el) { _add(el); }
        public void prepend(T el) { _prepend(el); }
    }

    // All the important member variables of *Node are public, so they are easily accessible from the
    // visitors. I am probably going to hell for this.

    // -------------   Type Nodes --------------------------------
    public class TypeNode : BaseNode {
        public TypeNode() : base() { }
        public TypeNode(params TypeNode[] t) : base(t) { }
    }
    public class TypeInt : TypeNode { }
    public class TypeBool : TypeNode { }
    public class TypeMachineID : TypeNode { }
    public class TypeModelMachineID : TypeNode { }
    public class TypeEventID : TypeNode { }
    public class TypeAny : TypeNode { }

    // For internal use only. Part of Dict/Named tuples
    public class TypeField : TypeNode
    {
        public string label;
        public TypeNode type;

        public TypeField(string l, TypeNode t)
            : base(t)
        {
            label = l;
            type = t;
        }
    }

    public class TypeNamedTuple : TypeNode {
        public void append(string name, TypeNode t)
        {
            _add(new TypeField(name, t));
        }

        public void prepend(string name, TypeNode t)
        {
            _prepend(new TypeField(name, t));
        }
    }

    public class TypeTuple : TypeNode
    {
        public void append(TypeNode t)
        {
            _add(t);
        }

        public void prepend(TypeNode t)
        {
            _prepend(t);
        }
    }

    public class TypeSeq : TypeNode
    {
        public TypeNode innerT;

        public TypeSeq(TypeNode innerT) :
            base(innerT)
        {
            this.innerT = innerT;
        }
    }

    public class TypeMap : TypeNode
    {
        public TypeNode domain;
        public TypeNode range;

        public TypeMap(TypeNode domain, TypeNode range) :
            base(domain, range)
        {
            this.domain = domain;
            this.range = range;
        }
    }

    // -------------   Declarative P Nodes -----------------------
    public interface IPDeclaration : INode { }

    public sealed class Program : BaseListNode<IPDeclaration> { }
    public sealed class EventDeclaration : BaseNode, IPDeclaration
    {
        public string id;
        public int assume, assert;
        public TypeNode payloadType;

        public EventDeclaration(string id, int assert, int assume)
            : base()
        {
            this.id = id;
            this.assert = assert;
            this.assume = assume;
            this.payloadType = null;
        }

        public EventDeclaration(string id, int assert, int assume, TypeNode payload) : base(payload)
        {
            this.id = id;
            this.assert = assert;
            this.assume = assume;
            this.payloadType = payload;
        }
    }

    public sealed class MachineDeclaration : BaseListNode<IMachineBodyItem>, IPDeclaration
    {
        public string id;
        public bool isMain;
        public string type;
        public IEnumerable<IMachineBodyItem> body;

        public MachineDeclaration(string id, bool main, string type, IEnumerable<IMachineBodyItem> body)
            : base(body)
        {
            this.id = id;
            this.isMain = main;
            this.type = type;
            this.body = body;
        }
    }

    public sealed class MonitorDeclaration : BaseListNode<IMachineBodyItem>, IPDeclaration
    {
        public string id;
        public IEnumerable<IMachineBodyItem> body;

        public MonitorDeclaration(string id, IEnumerable<IMachineBodyItem> body)
            : base(body)
        {
            this.id = id;
            this.body = body;
        }
    }

    // -------------   Machine Body Item -------------------------
    public interface IMachineBodyItem : INode { }

    public sealed class MaxQueueDeclaration : BaseNode, IMachineBodyItem
    {
        public int size;

        public MaxQueueDeclaration(int size) : base()
        {
            this.size = size;
        }
    }

    public sealed class VarDeclaration : BaseNode, IMachineBodyItem
    {
        public TypeNode type;
        public string name;
        public bool isGhost;

        public VarDeclaration(TypeNode type, string name, bool ghost) : base(type) {
            this.type = type;
            this.name = name;
            this.isGhost = ghost;
        }
    }

    public sealed class FunDeclaration : BaseNode, IMachineBodyItem
    {
        public string name;
        public bool isModel;
        public TypeNamedTuple paramTypes;
        public TypeNode returnType;
        public DSLBlock body;
        public DSLAttribute passiveAttr;

        public FunDeclaration(string name, bool isModel, TypeNamedTuple paramTypes, TypeNode retType, DSLBlock body, DSLAttribute attribute)
            : base(paramTypes, retType, body, attribute)
        {
            this.name = name;
            this.isModel = isModel;
            this.paramTypes = paramTypes;
            this.returnType = retType;
            this.body = body;
            this.passiveAttr = attribute;
        }
    }

    public sealed class StateDeclaration : BaseListNode<IStateBodyItem>, IMachineBodyItem
    {
        public string id;
        public bool isStart;
        public bool isStable;
        public StateDeclaration(string sid, IEnumerable<IStateBodyItem> body, bool start, bool stable) : base(body) {
            id = sid;
            isStart = start;
            isStable = stable;
        }
    }

    public sealed class SubmachineDeclaration : BaseListNode<StateDeclaration>, IMachineBodyItem
    {
        public string id;
        public SubmachineDeclaration(string sid, IEnumerable<StateDeclaration> body)
            : base(body)
        {
            id = sid;
        }
    }

    public sealed class ActionDeclaration : BaseNode, IMachineBodyItem
    {
        public string name;
        public DSLBlock body;

        public ActionDeclaration(string n, DSLBlock b)
            : base(b)
        {
            name = n;
            body = b;
        }
    }


    // -------------   State Body Nodes --------------------------
    public interface IStateBodyItem : INode { }

    public class StateFunction : BaseNode, IStateBodyItem
    {
        public DSLBlock body;

        public StateFunction(DSLBlock body)
            : base(body)
        {
            this.body = body;
        }
    }

    public sealed class EntryFunction : StateFunction {
        public EntryFunction(DSLBlock body) : base(body) { }
    }
    public sealed class ExitFunction : StateFunction {
        public ExitFunction(DSLBlock body) : base(body) { }
    }

    public class EventSet : BaseNode, IStateBodyItem {
        public List<string> events;
        
        public EventSet(List<string> events) : base() { this.events = events; }
    }

    public sealed class Defer : EventSet {
        public Defer(List<string> events) : base(events) { }
    }
    public sealed class Ignore : EventSet {
        public Ignore(List<string> events) : base(events) { }
    }

    public class BaseTransition : BaseNode, IStateBodyItem
    {
        public List<string> on;
        public string targetState;

        public BaseTransition(List<string> on, string target, DSLBlock block)
            : base(block)
        {
            this.on = on;
            this.targetState = target;
        }
    }

    public sealed class Transition : BaseTransition {
        public DSLBlock block;
        public Transition(List<string> on, string target, DSLBlock block) : base(on, target, block) 
        { 
            this.block = block;
        }
    }

    public sealed class CallTransition : BaseTransition {
        public CallTransition(List<string> on, string target) : base(on, target, null) { }
    }

    public sealed class Action : BaseNode, IStateBodyItem
    {
        public List<string> on;
        public string action;

        public Action(List<string> evts, string actionName)
            : base()
        {
            on = evts;
            action = actionName;
        }
    }

    
    // -------------   Functional Language Nodes -----------------
    // We use interfaces as tags for certain node types
    public interface IDSLNode : INode { }
    public interface IDSLStmt : IDSLNode { }
    public interface IDSLExp : IDSLNode { }
    public interface IDSLLhs : IDSLExp { };

    // -------------   Statement Nodes ---------------------------
    public sealed class DSLITE : BaseNode, IDSLStmt
    {
        public IDSLExp c;
        public IDSLStmt sTrue, sFalse;

        public DSLITE(IDSLExp c, IDSLStmt sTrue, IDSLStmt sFalse) : base(c, sTrue, sFalse)
        {
            this.c = c; this.sTrue = sTrue; this.sFalse = sFalse;
        }
    }

    public sealed class DSLWhile : BaseNode, IDSLStmt
    {
        public IDSLExp c; 
        public IDSLStmt body;

        public DSLWhile(IDSLExp c, IDSLStmt body) : base(c,body)
        {
            this.c = c; this.body = body;
        }
    }

    public sealed class DSLAssign : BaseNode, IDSLStmt
    {
        public IDSLExp lhs;
        public IDSLExp rhs;

        public DSLAssign(IDSLExp lhs, IDSLExp rhs) : base(lhs, rhs)
        {
            this.lhs = lhs; this.rhs = rhs;
        }
    }

    public sealed class DSLBlock : BaseListNode<IDSLStmt>, IDSLStmt { }
    
    // Chose DSLStmt as base type, since of all call-like things, only Foreign Func Calls appear in expressions.
    public sealed class DSLFFCallStmt : BaseNode, IDSLStmt 
    {
        public DSLFFCall fcall;
        public DSLFFCallStmt(DSLFFCall f) : base(f)
        {
            fcall = f;
        }
    }

   public class DSLSend : BaseNode, IDSLStmt
    {
       public IDSLExp target;
       public IDSLExp evt;
       public IDSLExp payload;

       public DSLSend(IDSLExp t, IDSLExp e, IDSLExp p) : base(t,e,p)
       {
           target = t; evt = e; payload = p;
       }
    }

    public class DSLSCall : BaseNode, IDSLStmt
    {
        public IDSLExp target;

        public DSLSCall(IDSLExp t) : base(t)
        {
            target = t;
        }
    }

    public class DSLMCall : BaseNode, IDSLStmt
    {
        public string monitorName;
        public IDSLExp evt;
        public IDSLExp arg;

        public DSLMCall(string monitorName, IDSLExp ev, IDSLExp arg)
            : base(ev, arg)
        {
            this.monitorName = monitorName;
            this.evt = ev;
            this.arg = arg;
        }
    }

    public class DSLRaise : BaseNode, IDSLStmt
    {
        public IDSLExp evt;
        public IDSLExp payload;

        public DSLRaise(IDSLExp e, IDSLExp p) : base(e, p)
        {
            evt = e; payload = p;
        }
    }

    public class DSLAssert : BaseNode, IDSLStmt
    {
        public IDSLExp cond;
        public DSLAssert(IDSLExp exp) : base(exp)
        {
            cond = exp;
        }
    }

    public sealed class DSLReturn : BaseNode, IDSLStmt
    {
        public IDSLExp rval;
        public DSLReturn(IDSLExp rval) : base(rval)
        {
            this.rval = rval;
        }
    }

    public sealed class DSLNewStmt : BaseNode, IDSLStmt
    {
        public IDSLExp newExp;
        public DSLNewStmt(IDSLExp newExp) : base(newExp)
        {
            this.newExp = newExp;
        }
    }
    
    public sealed class DSLLeave : BaseNode, IDSLStmt { }
    public sealed class DSLSkip : BaseNode, IDSLStmt { }

    //-------------------- Expression Nodes ----------------------------------------
    public enum Ops
    {
        U_MINUS, U_LNOT,

        B_MUL, B_DIV,
        B_PLUS, B_MINUS,
        B_LT, B_GT, B_LE, B_GE, B_IN,
        B_EQ, B_NE,
        B_LAND,
        B_LOR
    }

    public sealed class DSLId : BaseNode, IDSLExp, IDSLLhs
    {
        public string id;
        public DSLId(string id) : base() { this.id = id; }
    }

    public sealed class DSLMember : BaseNode, IDSLExp, IDSLLhs
    {
        public string member;
        public IDSLExp baseExp;
        public DSLMember(IDSLExp baseE, string s) : base(baseE)
        {
            this.member = s;
            this.baseExp = baseE;
        }
    }

    public sealed class DSLIndex : BaseNode, IDSLExp, IDSLLhs
    {
        public IDSLExp indexExp;
        public IDSLExp baseExp;
        public DSLIndex(IDSLExp baseE, IDSLExp indexE)
            : base(baseE, indexE)
        {
            this.indexExp = indexE;
            this.baseExp = baseE;
        }
    }

    public sealed class DSLArg : BaseNode, IDSLExp, IDSLLhs
    {
        public TypeNode type;
        public DSLArg(TypeNode t)
            : base(t)
        {
            type = t;
        }
    }

    public sealed class DSLInt : BaseNode, IDSLExp
    {
        public int v;
        public DSLInt(int v) : base() { this.v = v; }
    }

    public sealed class DSLBool : BaseNode, IDSLExp
    {
        public bool v;
        public DSLBool(bool v) : base() { this.v = v; }
    }

    public sealed class DSLFFCall : BaseNode, IDSLExp
    {        
        public string fname;
        public DSLTuple args;
        public bool isExternalCall;

        public DSLFFCall(string fname, DSLTuple args) : base(args)
        {
            this.fname = fname; 
            this.args = args;
            this.isExternalCall = fname.StartsWith("__");
        }
    }
    
    public sealed class DSLUnop : BaseNode, IDSLExp
    {
        public Ops op;
        public IDSLExp e;

        public DSLUnop(Ops op, IDSLExp e) : base(e)
        {
            this.op = op; this.e = e;
        }
    }

    public sealed class DSLBinop : BaseNode, IDSLExp
    {
        public Ops op;
        public IDSLExp e1, e2;

        public DSLBinop(Ops op, IDSLExp e1, IDSLExp e2) : base(e1, e2)
        {
            this.op = op; this.e1 = e1; this.e2 = e2;
        }
    }

    public sealed class DSLTuple : BaseListNode<IDSLExp>, IDSLExp { }

    public sealed class DSLKWArgs : BaseNode, IDSLExp
    {
        public Dictionary<string, IDSLExp> els;

        public DSLKWArgs()
        {
            this.els = new Dictionary<string, IDSLExp>();
        }

        public void set(string k, IDSLExp v) { els[k] = v; }
        public IDSLExp get(string k) { return els[k]; }

        public override IEnumerable<INode> children
        {
            get
            {
                foreach (KeyValuePair<string, IDSLExp> p in els)
                {
                    yield return (INode)p.Value;
                }
            }
        }
    }

    public sealed class DSLNamedTuple : BaseNode, IDSLExp
    {
        public List<Tuple<string, IDSLExp>> els;

        public DSLNamedTuple() : base()
        {
            this.els = new List<Tuple<string,IDSLExp>>();
        }

        public void append(string k, IDSLExp v) { els.Add(new Tuple<string, IDSLExp>(k, v)); }
        public void prepend(string k, IDSLExp v) { els.Insert(0, new Tuple<string, IDSLExp>(k, v)); }

        public override IEnumerable<INode> children
        {
            get
            {
                foreach (Tuple<string, IDSLExp> p in els)
                {
                    yield return (INode)p.Item2;
                }
            }
        }
    }

    public sealed class DSLNew : BaseNode, IDSLExp
    {
        public string machineName;
        public IDSLExp arg;

        public DSLNew(string name, IDSLExp arg) : base(arg)
        {
            this.machineName = name; this.arg = arg;
        }
    }

    public sealed class DSLAttribute : BaseNode
    {
        public string name;

        public DSLAttribute(string name) : base()
        {
            this.name = name;
        }
    }

    public sealed class DSLSizeof : BaseNode, IDSLExp
    {
        public IDSLExp of;

        public DSLSizeof(IDSLExp of)
            : base(of)
        {
            this.of = of;
        }
    }

    public sealed class DSLKeys : BaseNode, IDSLExp
    {
        public IDSLExp of;

        public DSLKeys(IDSLExp of)
            : base(of)
        {
            this.of = of;
        }
    }

    public sealed class DSLMutation : BaseNode, IDSLStmt
    {
        public IDSLExp baseE;
        public string op;
        public DSLTuple args;

        public DSLMutation(IDSLExp baseE, string op, DSLTuple args)
            : base(baseE, args)
        {
            this.baseE = baseE;
            this.op = op;
            this.args = args;
        }
    }

    public sealed class DSLSeq : BaseListNode<IDSLExp>, IDSLExp
    {
        public DSLSeq(IEnumerable<IDSLExp> els) : base(els) { }
    }
}