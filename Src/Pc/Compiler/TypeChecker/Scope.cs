using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;

namespace Microsoft.Pc.TypeChecker
{
    // Note: the lack of generic support over method out parameters makes this class very large.
    // Not much is actually going on here, though.
    public class Scope
    {
        private readonly HashSet<Scope> children = new HashSet<Scope>();
        private readonly ITranslationErrorHandler handler;

        private readonly IDictionary<string, EnumElem> enumElems = new Dictionary<string, EnumElem>();
        private readonly IDictionary<string, PEnum> enums = new Dictionary<string, PEnum>();
        private readonly IDictionary<string, PEvent> events = new Dictionary<string, PEvent>();
        private readonly IDictionary<string, EventSet> eventSets = new Dictionary<string, EventSet>();
        private readonly IDictionary<string, Function> functions = new Dictionary<string, Function>();
        private readonly IDictionary<string, Interface> interfaces = new Dictionary<string, Interface>();
        private readonly IDictionary<string, Machine> machines = new Dictionary<string, Machine>();
        private readonly IDictionary<string, StateGroup> stateGroups = new Dictionary<string, StateGroup>();
        private readonly IDictionary<string, State> states = new Dictionary<string, State>();
        private readonly IDictionary<string, TypeDef> typedefs = new Dictionary<string, TypeDef>();
        private readonly IDictionary<string, Variable> variables = new Dictionary<string, Variable>();

        public Scope(ITranslationErrorHandler handler) : this(handler, null) { }

        public Scope(ITranslationErrorHandler handler, Scope parent)
        {
            this.handler = handler;
            parent?.children.Remove(this);
            Parent = parent;
            parent?.children.Add(this);
        }

        public Scope Parent { get; set; }

        public Scope MakeChildScope() { return new Scope(handler, this); }

        public IEnumerable<Scope> Children => children;

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
                     .Concat(Variables);

        public IEnumerable<EnumElem> EnumElems => enumElems.Values;
        public IEnumerable<PEnum> Enums => enums.Values;
        public IEnumerable<PEvent> Events => events.Values;
        public IEnumerable<EventSet> EventSets => eventSets.Values;
        public IEnumerable<Function> Functions => functions.Values;
        public IEnumerable<Interface> Interfaces => interfaces.Values;
        public IEnumerable<Machine> Machines => machines.Values;
        public IEnumerable<StateGroup> StateGroups => stateGroups.Values;
        public IEnumerable<State> States => states.Values;
        public IEnumerable<TypeDef> Typedefs => typedefs.Values;
        public IEnumerable<Variable> Variables => variables.Values;

        #region Overloaded getters

        public bool Get(string name, out EnumElem tree) { return enumElems.TryGetValue(name, out tree); }

        public bool Get(string name, out PEnum tree) { return enums.TryGetValue(name, out tree); }

        public bool Get(string name, out PEvent tree) { return events.TryGetValue(name, out tree); }

        public bool Get(string name, out EventSet tree) { return eventSets.TryGetValue(name, out tree); }

        public bool Get(string name, out Function tree) { return functions.TryGetValue(name, out tree); }

        public bool Get(string name, out Interface tree) { return interfaces.TryGetValue(name, out tree); }
        
        public bool Get(string name, out Machine tree) { return machines.TryGetValue(name, out tree); }

        public bool Get(string name, out StateGroup tree) { return stateGroups.TryGetValue(name, out tree); }

        public bool Get(string name, out State tree) { return states.TryGetValue(name, out tree); }

        public bool Get(string name, out TypeDef tree) { return typedefs.TryGetValue(name, out tree); }

        public bool Get(string name, out Variable tree) { return variables.TryGetValue(name, out tree); }

        #endregion

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

        public bool Lookup(string name, out EventSet tree)
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

        #endregion

        #region Conflict-checking putters

        public TypeDef Put(string name, PParser.PTypeDefContext tree)
        {
            var typedef = new TypeDef(name, tree);
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
            var typedef = new TypeDef(name, tree);
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
            var @enum = new PEnum(name, tree);
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
            var @event = new PEvent(name, tree);
            CheckConflicts(@event, Namespace(events), Namespace(enumElems));
            events.Add(name, @event);
            return @event;
        }

        public EventSet Put(string name, PParser.EventSetDeclContext tree)
        {
            var eventSet = new EventSet(name, tree);
            CheckConflicts(eventSet, Namespace(eventSets));
            eventSets.Add(name, eventSet);
            return eventSet;
        }

        public Interface Put(string name, PParser.InterfaceDeclContext tree)
        {
            var machineInterface = new Interface(name, tree);
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
            var machine = new Machine(name, tree);
            CheckConflicts(machine, Namespace(machines), Namespace(interfaces), Namespace(enums), Namespace(typedefs));
            machines.Add(name, machine);
            return machine;
        }
        
        public Machine Put(string name, PParser.SpecMachineDeclContext tree)
        {
            var specMachine = new Machine(name, tree);
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
            var function = new Function(name, tree);
            CheckConflicts(function, Namespace(functions));
            functions.Add(name, function);
            return function;
        }
        
        public StateGroup Put(string name, PParser.GroupContext tree)
        {
            var group = new StateGroup(name, tree);
            CheckConflicts(group, Namespace(stateGroups));
            stateGroups.Add(name, group);
            return group;
        }

        public EnumElem Put(string name, PParser.EnumElemContext tree)
        {
            var enumElem = new EnumElem(name, tree);
            CheckConflicts(enumElem, Namespace(enumElems), Namespace(events));
            enumElems.Add(name, enumElem);
            return enumElem;
        }

        public EnumElem Put(string name, PParser.NumberedEnumElemContext tree)
        {
            var enumElem = new EnumElem(name, tree);
            CheckConflicts(enumElem, Namespace(enumElems), Namespace(events));
            enumElems.Add(name, enumElem);
            return enumElem;
        }

        public Variable Put(string name, PParser.VarDeclContext tree, PParser.IdenContext varName)
        {
            var variable = new Variable(name, tree, varName);
            CheckConflicts(variable, Namespace(variables));
            variables.Add(name, variable);
            return variable;
        }

        public Variable Put(string name, PParser.FunParamContext tree)
        {
            var variable = new Variable(name, tree);
            CheckConflicts(variable, Namespace(variables));
            variables.Add(name, variable);
            return variable;
        }

        public State Put(string name, PParser.StateDeclContext tree)
        {
            var state = new State(name, tree);
            CheckConflicts(state, Namespace(states));
            states.Add(name, state);
            return state;
        }

        #endregion

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

        #endregion
    }
}
