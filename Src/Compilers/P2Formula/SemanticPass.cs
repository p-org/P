using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PParser
{
    public class SemanticError : BaseError
    {
        public SemanticError(DSLLoc l, string m) : base(l, m) { }
    }
    /// <summary>
    /// Semantic pass on the AST. Runs right before the type checker. It
    /// serves two purposes:
    /// 1) Compute the set of symbols in scope at each node in the AST. This includes
    ///     - all declared events
    ///     - all declared machines
    ///     - all declared states per machine
    ///     - all declared variables per machine
    ///     - all declared functions per machine
    ///     
    /// 2) Checks for the following error conditions:
    ///     - not exactly 1 main
    ///     - not exactly 1 start declaration per machine
    ///     - duplicate identifier declarations:
    ///         - any 2 events with the same name
    ///         - any 2 machines with the same name
    ///         - any 2 states in a machine, with the same name
    ///         - any 2 functions in a machine with the same name
    ///         - any 2 variables in a machine with the same name
    ///     - correct LHS expressions
    ///     - overlapping ignore/defer/action event sets for a given state.
    /// </summary>
    class SemanticPass : NoopVisitor<SemanticPass.BaseScope>
    {
        /// <summary>
        /// Simple nested Scopes class. Can define multiple "types" of symbols. (e.g.  Names, Event Names etc..)
        /// </summary>
        internal abstract class BaseScope
        {
            public class Symbol
            {
                public DSLLoc loc;
                public string name;
                public string type;
                public INode n;
                public BaseScope scope;

                public Symbol(string name, string t, DSLLoc loc, INode n, BaseScope scope)
                {
                    this.loc = loc; this.name = name; this.type = t; this.n = n; this.scope = scope;
                }

                public string resolvedName()
                {
                    BaseScope s = scope;
                    string rName = name;

                    while (s != null && !(s is GlobalScope))
                    {
                        rName = s.name + "_" + rName;
                        s = s.parent;
                    }

                    return rName;
                }
            }

            protected BaseScope pt;
            private Dictionary<string, Symbol> syms;
            private string scopeName;
            private HashSet<string> typesIHandle;

            public string name { get { return scopeName; } }
            public BaseScope parent { get { return pt; } set { pt = value; } }

            public BaseScope(string sName, string[] types)
            {
                this.pt = null;
                scopeName = sName;
                syms = new Dictionary<string, Symbol>();
                typesIHandle = new HashSet<string>(types);
            }

            private bool iHandle(string type)
            {
                return typesIHandle.Contains(type);
            }

            public void define(string name, string type, DSLLoc l, INode n)
            {
                if (!iHandle(type))
                    throw new NotImplementedException("Can't define symbol " + name + " of type " + type + " in current scope.(" + this.name + ")");

                syms[name] = new Symbol(name, type, l, n, this);
            }

            public Symbol lookupHere(string name)
            {
                if (syms.ContainsKey(name))
                        return syms[name];
                    else
                        return null;
            }

            public bool definedHere(string name)
            {
                return lookupHere(name) != null;
            }

            public bool defined(string name)
            {
                return lookup(name) != null;
            }

            public bool defined(string name, string type)
            {
                return lookup(name) != null &&
                    lookup(name).type == type;
            }

            public Symbol lookup(string name)
            {
                if (definedHere(name))
                    return lookupHere(name);
                else
                {
                    if (pt != null)
                        return pt.lookup(name);
                    else
                        return null;
                }
            }
        }

        public const string SYM_EVENT = "event";
        public const string SYM_MACHINE = "machine";
        public const string SYM_VAR = "var";
        public const string SYM_STATE = "state";
        public const string SYM_SUBMACHINE = "submachine";
        public const string SYM_FUN = "fun";
        public const string SYM_ACTION = "action";
        public const string SYM_BUILTIN_VAR = "builtin_var";
        public const string SYM_PARAM = "param";

        public const string VAR_THIS = "this";
        public const string VAR_PAYLOAD = "payload";
        public const string VAR_TRIGGER = "trigger";
        public const string VAR_DEFAULT = "default";
        public const string VAR_CHOICE = "*";
        public const string VAR_NULL = "null";
        public const string VAR_IGNORE_ACTION = "ignore";

        // Dummy symbols types, for internal use only!
        private const string VAR_MAIN = "<main>";
        private const string SYM_MAIN = "main";
        private const string VAR_START = "<start>";
        private const string SYM_START = "start";
        private const string SYM_EVENT_USE = "event_use";
        private const string VAR_MAXQSIZE = "";
        private const string SYM_MAXQSIZE = "maxqueue";
        private const string SYM_STABLE = "stable";

        internal class GlobalScope : BaseScope
        {
            public GlobalScope() : base("global", new string[]{SYM_EVENT, SYM_MACHINE, SYM_MAIN, SYM_BUILTIN_VAR}) { }
        }

        internal class MachineScope : BaseScope
        {
            public MachineScope(string name) : base("machine_" + name, new string[] { SYM_VAR, SYM_STATE, SYM_FUN, SYM_ACTION, SYM_START, SYM_MAXQSIZE, SYM_SUBMACHINE}) { }
        }

        internal class SubmachineScope : BaseScope
        {
            public SubmachineScope(string name) : base("submachine_" + name, new string[] { SYM_STATE, SYM_START }) { }
        }

        internal class StateScope : BaseScope
        {
            public StateScope(string name) : base("state_" + name, new string[] { SYM_EVENT_USE, SYM_EVENT }) { }
        }

        /// <summary>
        /// Scope for exit/entry/foreign functions and actions. Currently the only symbols here 
        /// are the implicitly defined this, arg, msg. This will change with local variables.
        /// </summary>
        internal class FunctionScope : BaseScope
        {
            public FunctionScope(string name) : base("function " + name, new string[] { SYM_VAR, SYM_BUILTIN_VAR, SYM_PARAM }) { }
        }

        List<SemanticError> errs;
        List<MachineScope> machineScopes;
        Dictionary<INode, BaseScope> scopeLookup;
        GlobalScope global;
        BaseScope current;

        public List<SemanticError> errors { get { return errs; } }

        public SemanticPass(Program p) : base()
        {
            errs = new List<SemanticError>();
            machineScopes = new List<MachineScope>();
            global = new GlobalScope();
            scopeLookup = new Dictionary<INode, BaseScope>();
            current = global;
            
            // Define global built in symbols (e.g. non-det choice)
            global.define(VAR_CHOICE, SYM_BUILTIN_VAR, new DSLLoc(null), null);
            global.define("null", SYM_BUILTIN_VAR, new DSLLoc(null), null);

            walk(p);

            // The visitors enforced we don't have more than one main per global/start per machine. Now we enforce there is at least one
            if (!global.defined(VAR_MAIN))
                errs.Add(new SemanticError(new DSLLoc(null), "Missing main expression."));

            foreach(MachineScope m in machineScopes)
                if (!m.defined(VAR_START))
                    errs.Add(new SemanticError(new DSLLoc(null), "Missing start for " + m.name));
        }

        public BaseScope getScope(INode n)
        {
            return scopeLookup[n];
        }

        public BaseScope.Symbol lookup(INode where, string s)
        {
            BaseScope scope = getPreResult(where);
            return scope.lookup(s);
        }

        public bool defined(INode where, string s, string symType)
        {
            var sym = lookup(where, s);
            return sym != null && sym.type == symType;
        }

        public string resolve(INode where, string s)
        {
            var sym = lookup(where, s);
            return sym.resolvedName();
        }

        // ------------------------ Semantic Pass Helpers -----------------------------------

        private bool errorDefined(string name, string type, DSLLoc l)
        {
            if (current.defined(name))
            {
                var s = current.lookup(name);
                errs.Add(new SemanticError(l, type + " " + name + " conflicts previous definition of  " + s.type + " " + s.name + " on line " + s.loc.startLine));
                return true;
            }
            else
                return false;
        }

        // Keep track of the events for which we've seen a Transition/Ignore/Defer/Action at the current scope
        // by defining a dummy symbol evtName + "_use". This prevents conflicting transitions, ignores, defers, etc..
        // A lil' hackish, I know.
        private void checkEventOverlapping(IEnumerable<string> set, INode n)
        {
            foreach (string evt in set)
            {
                var evtUse = "<" + evt + "_use>";
                if (current.defined(evtUse))
                {
                    errs.Add(new SemanticError(n.loc, "multiple possible outcomes for event " + evt + " in " + current.name +
                        " e.g. see " + n.loc.str() + " and " + current.lookup(evtUse).loc.str()));
                }
                else
                    current.define(evtUse, SYM_EVENT_USE, n.loc, n);
            }
        }

        /// <summary>
        /// Push a new scope on, and return the old topmost one.
        /// </summary>
        /// <param name="newS"></param>
        /// <returns></returns>
        private BaseScope pushScope(BaseScope newS, INode n)
        {
            var oldS = current;
            newS.parent = current;
            current = newS;
            scopeLookup[n] = newS;

            return oldS;
        }

        private BaseScope popScope()
        {
            var oldS = current;
            current = current.parent;
            return oldS;
        }

        // ------------------------ Visitors -----------------------------------------------
        public override BaseScope visit_pre(EventDeclaration n)
        {
            if (!errorDefined(n.id, SYM_EVENT, n.loc))
                current.define(n.id, SYM_EVENT, n.loc, n);
            return current;
        }

        public override BaseScope visit_pre(MachineDeclaration n)
        {
            // Note that machine definitions themselves are checked in their parent scope, after which
            // we push the scope they define.
            if (!errorDefined(n.id, SYM_MACHINE, n.loc))
                current.define(n.id, SYM_MACHINE, n.loc, n);

            if (n.isMain)
            {
                if (current.defined(VAR_MAIN))
                {
                    errs.Add(new SemanticError(n.loc, "Multiple main expressions found."));
                }
                else
                    current.define(VAR_MAIN, SYM_MAIN, n.loc, n);
            }

            var newS = new MachineScope(n.id);

            
            // All machine implicitly have an Ignore Action
            newS.define(VAR_IGNORE_ACTION, SemanticPass.SYM_ACTION, n.loc, n);
            machineScopes.Add(newS);
            return pushScope(newS, n);                           // return old scope as machine declaration's scope
        }
        public override BaseScope visit(MachineDeclaration n) { return popScope(); }

        public override BaseScope visit_pre(StateDeclaration n)
        {
            // NOTE: See comments for visit_pre(MachineDeclaration)
            if (!errorDefined(n.id, SYM_STATE, n.loc))
                current.define(n.id, SYM_STATE, n.loc, n);

            if (n.isStart)
            {
                if (current.defined(VAR_START))   // Piggybacking on the scope symbol naming mechanism to enforce at most one start statement per machine
                {
                    var s = current.lookup(VAR_START);
                    var machName = current is MachineScope ? current.name : current.parent.name;
                    errs.Add(new SemanticError(n.loc, machName + " contains multiple start statements. Previous statement was on line " + s.loc.startLine));
                }
                else
                {
                    if (current is MachineScope)
                        current.define(VAR_START, SYM_START, n.loc, n);
                    else
                    {
                        Debug.Assert(current is SubmachineScope);
                        current.parent.define(VAR_START, SYM_START, n.loc, n);
                    }
                }
            }

            var newS = new StateScope(n.id);

            if (!errorDefined(VAR_DEFAULT, SYM_EVENT, n.loc))
                newS.define(VAR_DEFAULT, SYM_EVENT, n.loc, n);

            return pushScope(newS, n);
        }
        public override BaseScope visit(StateDeclaration n) { return popScope(); }

        public override BaseScope visit_pre(SubmachineDeclaration n)
        {
            // NOTE: See comments for visit_pre(MachineDeclaration)
            if (!errorDefined(n.id, SYM_SUBMACHINE, n.loc))
                current.define(n.id, SYM_SUBMACHINE, n.loc, n);

            var newS = new SubmachineScope(n.id);
            return pushScope(newS, n);                           // return old scope as machine declaration's scope
        }

        public override BaseScope visit(SubmachineDeclaration n) { return popScope(); }

        public override BaseScope visit_pre(VarDeclaration n)
        {
            if (!errorDefined(n.name, SYM_VAR, n.loc))
                current.define(n.name, SYM_VAR, n.loc, n);

            return current;
        }

        public override BaseScope visit_pre(MaxQueueDeclaration n)
        {
            // Ensure there is a unique maxqueuesize declaration per machine.
            if (!errorDefined(VAR_MAXQSIZE, SYM_MAXQSIZE, n.loc))
                current.define(VAR_MAXQSIZE, SYM_MAXQSIZE, n.loc, n);

            if (n.size < 0)
            {
                errors.Add(new SemanticError(n.loc, "Can't specify negative max queue sizes."));
            }

            return current;
        }

        public override BaseScope visit_pre(FunDeclaration n)
        {
            if (!errorDefined(n.name, SYM_FUN, n.loc))
                current.define(n.name, SYM_FUN, n.loc, n);

            var fs = new FunctionScope(n.name);

            if (!errorDefined(VAR_THIS, SYM_BUILTIN_VAR, n.loc))
                fs.define(VAR_THIS, SYM_BUILTIN_VAR, n.loc, n);

            if (n.name.StartsWith("__"))
            {
                errors.Add(new SemanticError(n.loc, "Function names cannot begin with __"));
            }

            if (n.passiveAttr != null)
            {
                if (n.passiveAttr.name != "passive")
                {
                    errors.Add(new SemanticError(n.loc, string.Format("Unknown attribute {0}. Expeceted either 'passive' or nothing", n.name)));
                }
            }

            if (n.paramTypes != null)
                foreach (INode arg in n.paramTypes.children)
                {
                    var param = (((TypeField)arg).label);
                    if (param == "Context" || param == "dst")
                        errors.Add(new SemanticError(n.loc,
                            string.Format("Cannot use a parameter name '{1}' in function {0}. The '{1}' parameter name is reserved for implicit parameters in emitted C code", n.name, param)));
        
                    fs.define(param, SYM_PARAM, n.loc, n);
                }

            return pushScope(fs, n);
        }
        public override BaseScope visit(FunDeclaration n) { return popScope(); }

        public override BaseScope visit_pre(ActionDeclaration n)
        {
            if (!errorDefined(n.name, SYM_ACTION, n.loc))
                current.define(n.name, SYM_ACTION, n.loc, n);

            var newS = new FunctionScope("action_" + n.name);

            // Define the implicit variables arg and msg for the action
            if (!errorDefined(VAR_PAYLOAD, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_PAYLOAD, SYM_BUILTIN_VAR, n.loc, n);

            if (!errorDefined(VAR_TRIGGER, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_TRIGGER, SYM_BUILTIN_VAR, n.loc, n);

            if (!errorDefined(VAR_THIS, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_THIS, SYM_BUILTIN_VAR, n.loc, n);

            return pushScope(newS, n);
        }
        public override BaseScope visit(ActionDeclaration n) { return popScope(); }

        private bool isAssignable(IDSLExp lhs)
        {
            if (lhs is DSLId)
                return true;

            if (lhs is DSLIndex)
                return isAssignable((lhs as DSLIndex).baseExp);

            if (lhs is DSLMember)
                return isAssignable((lhs as DSLMember).baseExp);

            return false;
        }

        public override BaseScope visit_pre(DSLAssign n)
        {
            if (!isAssignable(n.lhs))
                errs.Add(new SemanticError(n.loc, "LHS is not assignable"));

            return current;
        }

        public override BaseScope visit_pre(Defer n)
        {
            checkEventOverlapping(n.events, n);
            return current;
        }

        public override BaseScope visit_pre(Ignore n)
        {
            checkEventOverlapping(n.events, n);
            return current;
        }

        public override BaseScope visit_pre(Transition n)
        {
            checkEventOverlapping(n.on, n);
            var newS = new FunctionScope("exit");

            // Define the implicit variables arg and msg for the action
            if (!errorDefined(VAR_PAYLOAD, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_PAYLOAD, SYM_BUILTIN_VAR, n.loc, n);

            if (!errorDefined(VAR_TRIGGER, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_TRIGGER, SYM_BUILTIN_VAR, n.loc, n);

            if (!errorDefined(VAR_THIS, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_THIS, SYM_BUILTIN_VAR, n.loc, n);

            return pushScope(newS, n);
        }
        public override BaseScope visit(Transition n)
        {
            return popScope();
        }

        public override BaseScope visit_pre(CallTransition n)
        {
            checkEventOverlapping(n.on, n);
            return current;
        }

        public override BaseScope visit_pre(Action n)
        {
            checkEventOverlapping(n.on, n);
            return current;
        }

        public override BaseScope visit_pre(ExitFunction n)
        {
            var newS = new FunctionScope("exit");

            // Define the implicit variables arg and msg for the action
            if (!errorDefined(VAR_PAYLOAD, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_PAYLOAD, SYM_BUILTIN_VAR, n.loc, n);

            if (!errorDefined(VAR_TRIGGER, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_TRIGGER, SYM_BUILTIN_VAR, n.loc, n);

            if (!errorDefined(VAR_THIS, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_THIS, SYM_BUILTIN_VAR, n.loc, n);

            return pushScope(newS, n);
        }
        public override BaseScope visit(ExitFunction n) { return popScope(); }
        public override BaseScope visit_pre(EntryFunction n)
        {
            var newS = new FunctionScope("entry");

            // Define the implicit variables arg and msg for the action
            if (!errorDefined(VAR_PAYLOAD, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_PAYLOAD, SYM_BUILTIN_VAR, n.loc, n);

            if (!errorDefined(VAR_TRIGGER, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_TRIGGER, SYM_BUILTIN_VAR, n.loc, n);

            if (!errorDefined(VAR_THIS, SYM_BUILTIN_VAR, n.loc))
                newS.define(VAR_THIS, SYM_BUILTIN_VAR, n.loc, n);

            return pushScope(newS, n);
        }
        public override BaseScope visit(EntryFunction n) { return popScope(); }

        public override BaseScope visit_pre(DSLNamedTuple n) {
            HashSet<string> fieldNames = new HashSet<string>();

            foreach (var f in n.els)
            {
                if (fieldNames.Contains(f.Item1))
                    errors.Add(new SemanticError(n.loc, "Tuple contains duplicate fields named " + f.Item1));
                else
                    fieldNames.Add(f.Item1);
            }
            return current;
        }

        public override BaseScope visit_pre(DSLMutation n)
        {
            if (n.op != "insert" && n.op != "remove" && n.op != "update")
            {
                errors.Add(new SemanticError(n.loc, "Unknown mutation operation on data structure: " + n.op));
            }
            return current;
        }

        public override BaseScope visit_pre(DSLId n) { return current; }
        // Remaining visitors just assign to each AST node its corresponding scope.
        public override BaseScope visit_pre(TypeField n) { return current; }
        public override BaseScope visit_pre(TypeNamedTuple n) { return current;  }
        public override BaseScope visit_pre(TypeMachineID n) { return current; }
        public override BaseScope visit_pre(TypeInt n) { return current; }
        public override BaseScope visit_pre(TypeEventID n) { return current; }
        public override BaseScope visit_pre(TypeBool n) { return current; }
        public override BaseScope visit_pre(TypeAny n) { return current; }
        public override BaseScope visit_pre(Program n) { return current; }
        public override BaseScope visit_pre(DSLWhile n) { return current; }
        public override BaseScope visit_pre(DSLUnop n) { return current; }
        public override BaseScope visit_pre(DSLTuple n) { return current; }
        public override BaseScope visit_pre(DSLSkip n) { return current; }
        public override BaseScope visit_pre(DSLSend n) { return current; }
        public override BaseScope visit_pre(DSLReturn n) { return current; }
        public override BaseScope visit_pre(DSLRaise n) { return current; }
        public override BaseScope visit_pre(DSLNew n) { return current; }
        public override BaseScope visit_pre(DSLKWArgs n) { return current; }
        public override BaseScope visit_pre(DSLITE n) { return current; }
        public override BaseScope visit_pre(DSLInt n) { return current; }
        public override BaseScope visit_pre(DSLMember n) { return current; }
        public override BaseScope visit_pre(DSLIndex n) { return current; }
        public override BaseScope visit_pre(DSLFFCallStmt n) { return current; }
        public override BaseScope visit_pre(DSLFFCall n) { return current; }
        public override BaseScope visit_pre(DSLLeave n) { return current; }
        public override BaseScope visit_pre(DSLSCall n) { return current; }
        public override BaseScope visit_pre(DSLBool n) { return current; }
        public override BaseScope visit_pre(DSLBlock n) { return current; }
        public override BaseScope visit_pre(DSLBinop n) { return current; }
        public override BaseScope visit_pre(DSLAssert n) { return current; }
    }
}