using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Plang.Compiler.TypeChecker
{
    // Note: the lack of generic support over method out parameters makes this class very large.
    // Not much is actually going on here, though.
    public class Scope
    {
        private readonly HashSet<Scope> children = new HashSet<Scope>();

        private readonly IDictionary<string, EnumElem> enumElems = new Dictionary<string, EnumElem>();
        private readonly IDictionary<string, PEnum> enums = new Dictionary<string, PEnum>();
        private readonly IDictionary<string, PEvent> events = new Dictionary<string, PEvent>();
        private readonly IDictionary<string, NamedEventSet> eventSets = new Dictionary<string, NamedEventSet>();
        private readonly IDictionary<string, Function> functions = new Dictionary<string, Function>();
        private readonly ITranslationErrorHandler handler;
        private readonly IDictionary<string, Implementation> implementations = new Dictionary<string, Implementation>();
        private readonly IDictionary<string, Interface> interfaces = new Dictionary<string, Interface>();
        private readonly IDictionary<string, Machine> machines = new Dictionary<string, Machine>();
        private readonly IDictionary<string, NamedModule> namedModules = new Dictionary<string, NamedModule>();
        private readonly IDictionary<string, RefinementTest> refinementTests = new Dictionary<string, RefinementTest>();
        private readonly IDictionary<string, SafetyTest> safetyTests = new Dictionary<string, SafetyTest>();
        private readonly IDictionary<string, StateGroup> stateGroups = new Dictionary<string, StateGroup>();
        private readonly IDictionary<string, State> states = new Dictionary<string, State>();
        private readonly IDictionary<string, TypeDef> typedefs = new Dictionary<string, TypeDef>();
        private readonly IDictionary<string, Variable> variables = new Dictionary<string, Variable>();

        private Scope(ITranslationErrorHandler handler, Scope parent = null)
        {
            this.handler = handler;
            parent?.children.Remove(this);
            Parent = parent;
            parent?.children.Add(this);

            EventSet eventSetWithHalt = new EventSet();
            eventSetWithHalt.AddEvent(new PEvent("halt", null));
            UniversalEventSet = parent == null ? eventSetWithHalt : parent.UniversalEventSet;
        }

        public EventSet UniversalEventSet { get; }

        private Scope Parent { get; }

        public IEnumerable<IPDecl> AllDecls =>
            EnumElems.Cast<IPDecl>()
                .Concat(Enums)
                .Concat(Events)
                .Concat(EventSets)
                .Concat(Functions)
                .Concat(Interfaces)
                .Concat(Machines)
                .Concat(StateGroups)
                .Concat(States)
                .Concat(Typedefs)
                .Concat(Variables)
                .Concat(Implementations)
                .Concat(SafetyTests)
                .Concat(RefinementTests)
                .Concat(NamedModules);

        public IEnumerable<EnumElem> EnumElems => enumElems.Values;
        public IEnumerable<PEnum> Enums => enums.Values;
        public IEnumerable<PEvent> Events => events.Values;
        public IEnumerable<NamedEventSet> EventSets => eventSets.Values;
        public IEnumerable<Function> Functions => functions.Values;
        public IEnumerable<Interface> Interfaces => interfaces.Values;
        public IEnumerable<Machine> Machines => machines.Values;
        public IEnumerable<StateGroup> StateGroups => stateGroups.Values;
        public IEnumerable<State> States => states.Values;
        public IEnumerable<TypeDef> Typedefs => typedefs.Values;
        public IEnumerable<Variable> Variables => variables.Values;
        public IEnumerable<SafetyTest> SafetyTests => safetyTests.Values;
        public IEnumerable<RefinementTest> RefinementTests => refinementTests.Values;
        public IEnumerable<Implementation> Implementations => implementations.Values;
        public IEnumerable<NamedModule> NamedModules => namedModules.Values;

        public static Scope CreateGlobalScope(ITranslationErrorHandler handler)
        {
            return new Scope(handler);
        }

        public Scope MakeChildScope()
        {
            return new Scope(handler, this);
        }

        public IEnumerable<Function> GetAllMethods()
        {
            foreach (Function fun in Functions)
            {
                yield return fun;
            }

            foreach (Machine machine in Machines)
            {
                foreach (Function method in machine.Methods)
                {
                    yield return method;
                }
            }
        }

        #region Add Default Impl. Declaration

        public void AddDefaultImpl(Implementation defaultImplDecl)
        {
            Debug.Assert(!implementations.Any());
            implementations.Add(defaultImplDecl.Name, defaultImplDecl);
        }

        #endregion Add Default Impl. Declaration

        #region Overloaded getters

        public bool Get(string name, out EnumElem tree)
        {
            return enumElems.TryGetValue(name, out tree);
        }

        public bool Get(string name, out PEnum tree)
        {
            return enums.TryGetValue(name, out tree);
        }

        public bool Get(string name, out PEvent tree)
        {
            return events.TryGetValue(name, out tree);
        }

        public bool Get(string name, out NamedEventSet tree)
        {
            return eventSets.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Function tree)
        {
            return functions.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Interface tree)
        {
            return interfaces.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Machine tree)
        {
            return machines.TryGetValue(name, out tree);
        }

        public bool Get(string name, out StateGroup tree)
        {
            return stateGroups.TryGetValue(name, out tree);
        }

        public bool Get(string name, out State tree)
        {
            return states.TryGetValue(name, out tree);
        }

        public bool Get(string name, out TypeDef tree)
        {
            return typedefs.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Variable tree)
        {
            return variables.TryGetValue(name, out tree);
        }

        public bool Get(string name, out SafetyTest tree)
        {
            return safetyTests.TryGetValue(name, out tree);
        }

        public bool Get(string name, out RefinementTest tree)
        {
            return refinementTests.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Implementation tree)
        {
            return implementations.TryGetValue(name, out tree);
        }

        public bool Get(string name, out NamedModule tree)
        {
            return namedModules.TryGetValue(name, out tree);
        }

        #endregion Overloaded getters

        #region Overloaded lookup methods

        public bool Lookup(string name, out EnumElem tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out PEnum tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out PEvent tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out NamedEventSet tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Function tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Interface tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Machine tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out StateGroup tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out State tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out TypeDef tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Variable tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out SafetyTest tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out RefinementTest tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Implementation tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out NamedModule tree)
        {
            Scope current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        #endregion Overloaded lookup methods

        #region Conflict-checking putters

        public TypeDef Put(string name, PParser.PTypeDefContext tree)
        {
            TypeDef typedef = new TypeDef(name, tree);
            CheckConflicts(typedef,
                Namespace(typedefs),
                Namespace(enums),
                Namespace(interfaces),
                Namespace(machines));
            typedefs.Add(name, typedef);
            return typedef;
        }

        internal TypeDef Put(string name, PParser.ForeignTypeDefContext tree)
        {
            TypeDef typedef = new TypeDef(name, tree);
            CheckConflicts(typedef,
                Namespace(typedefs),
                Namespace(enums),
                Namespace(interfaces),
                Namespace(machines));
            typedefs.Add(name, typedef);
            return typedef;
        }

        public PEnum Put(string name, PParser.EnumTypeDefDeclContext tree)
        {
            PEnum @enum = new PEnum(name, tree);
            CheckConflicts(
                @enum,
                Namespace(enums),
                Namespace(interfaces),
                Namespace(typedefs),
                Namespace(machines));
            enums.Add(name, @enum);
            return @enum;
        }

        public PEvent Put(string name, PParser.EventDeclContext tree)
        {
            PEvent @event = new PEvent(name, tree);
            CheckConflicts(@event, Namespace(events), Namespace(enumElems));
            events.Add(name, @event);
            return @event;
        }

        public NamedEventSet Put(string name, PParser.EventSetDeclContext tree)
        {
            NamedEventSet eventSet = new NamedEventSet(name, tree);
            CheckConflicts(eventSet, Namespace(eventSets), Namespace(interfaces));
            eventSets.Add(name, eventSet);
            return eventSet;
        }

        public Interface Put(string name, PParser.InterfaceDeclContext tree)
        {
            Interface machineInterface = new Interface(name, tree);
            CheckConflicts(
                machineInterface,
                Namespace(interfaces),
                Namespace(enums),
                Namespace(typedefs),
                Namespace(machines));
            interfaces.Add(name, machineInterface);
            return machineInterface;
        }

        public Machine Put(string name, PParser.ImplMachineDeclContext tree)
        {
            Machine machine = new Machine(name, tree);
            CheckConflicts(machine, Namespace(machines), Namespace(interfaces), Namespace(enums), Namespace(typedefs));
            machines.Add(name, machine);
            interfaces.Add(name, new Interface(name, tree));
            return machine;
        }

        public Machine Put(string name, PParser.SpecMachineDeclContext tree)
        {
            Machine specMachine = new Machine(name, tree);
            CheckConflicts(
                specMachine,
                Namespace(machines),
                Namespace(interfaces),
                Namespace(enums),
                Namespace(typedefs));
            machines.Add(name, specMachine);
            return specMachine;
        }

        public Function Put(string name, PParser.FunDeclContext tree)
        {
            Function function = new Function(name, tree);
            CheckConflicts(function, Namespace(functions));
            functions.Add(name, function);
            return function;
        }

        public StateGroup Put(string name, PParser.GroupContext tree)
        {
            StateGroup group = new StateGroup(name, tree);
            CheckConflicts(group, Namespace(stateGroups));
            stateGroups.Add(name, group);
            return group;
        }

        public EnumElem Put(string name, PParser.EnumElemContext tree)
        {
            EnumElem enumElem = new EnumElem(name, tree);
            CheckConflicts(enumElem, Namespace(enumElems), Namespace(events));
            enumElems.Add(name, enumElem);
            return enumElem;
        }

        public EnumElem Put(string name, PParser.NumberedEnumElemContext tree)
        {
            EnumElem enumElem = new EnumElem(name, tree);
            CheckConflicts(enumElem, Namespace(enumElems), Namespace(events));
            enumElems.Add(name, enumElem);
            return enumElem;
        }

        public Variable Put(string name, ParserRuleContext tree, VariableRole role)
        {
            Variable variable = new Variable(name, tree, role);
            CheckConflicts(variable, Namespace(variables));
            variables.Add(name, variable);
            return variable;
        }

        public State Put(string name, PParser.StateDeclContext tree)
        {
            State state = new State(tree, name);
            CheckConflicts(state, Namespace(states));
            states.Add(name, state);
            return state;
        }

        public NamedModule Put(string name, PParser.NamedModuleDeclContext tree)
        {
            NamedModule namedModule = new NamedModule(tree, name);
            CheckConflicts(namedModule, Namespace(namedModules));
            namedModules.Add(name, namedModule);
            return namedModule;
        }

        public Implementation Put(string name, PParser.ImplementationDeclContext tree)
        {
            Implementation impl = new Implementation(tree, name);
            CheckConflicts(impl,
                Namespace(implementations),
                Namespace(safetyTests),
                Namespace(refinementTests));
            implementations.Add(name, impl);
            return impl;
        }

        public SafetyTest Put(string name, PParser.SafetyTestDeclContext tree)
        {
            SafetyTest safetyTest = new SafetyTest(tree, name);
            CheckConflicts(safetyTest,
                Namespace(implementations),
                Namespace(safetyTests),
                Namespace(refinementTests));
            safetyTests.Add(name, safetyTest);
            return safetyTest;
        }

        public RefinementTest Put(string name, PParser.RefinementTestDeclContext tree)
        {
            RefinementTest refineTest = new RefinementTest(tree, name);
            CheckConflicts(refineTest,
                Namespace(implementations),
                Namespace(safetyTests),
                Namespace(refinementTests));
            refinementTests.Add(name, refineTest);
            return refineTest;
        }

        #endregion Conflict-checking putters

        #region Conflict API

        // TODO: maybe optimize this?
        private delegate bool TableReader(string name, out IPDecl decl);

        private void CheckConflicts(IPDecl decl, params TableReader[] namespaces)
        {
            IPDecl existingDecl = null;
            if (namespaces.Any(table => table(decl.Name, out existingDecl)))
            {
                throw handler.DuplicateDeclaration(decl.SourceLocation, decl, existingDecl);
            }
        }

        private static TableReader Namespace<T>(IDictionary<string, T> table) where T : IPDecl
        {
            return (string name, out IPDecl decl) =>
            {
                bool success = table.TryGetValue(name, out T tDecl);
                decl = tDecl;
                return success;
            };
        }

        #endregion Conflict API
    }
}