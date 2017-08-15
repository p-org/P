using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclarationTable
    {
        private readonly IDictionary<string, EnumElem> enumElems = new Dictionary<string, EnumElem>();
        private readonly IDictionary<string, PEnum> enums = new Dictionary<string, PEnum>();
        private readonly IDictionary<string, PEvent> events = new Dictionary<string, PEvent>();
        private readonly IDictionary<string, EventSet> eventSets = new Dictionary<string, EventSet>();
        private readonly IDictionary<string, FunctionProto> functionProtos = new Dictionary<string, FunctionProto>();
        private readonly IDictionary<string, Function> functions = new Dictionary<string, Function>();
        private readonly IDictionary<string, Interface> interfaces = new Dictionary<string, Interface>();
        private readonly IDictionary<string, MachineProto> machineProtos = new Dictionary<string, MachineProto>();
        private readonly IDictionary<string, Machine> machines = new Dictionary<string, Machine>();
        private readonly IDictionary<string, StateGroup> stateGroups = new Dictionary<string, StateGroup>();
        private readonly IDictionary<string, State> states = new Dictionary<string, State>();
        private readonly IDictionary<string, TypeDef> typedefs = new Dictionary<string, TypeDef>();
        private readonly IDictionary<string, Variable> variables = new Dictionary<string, Variable>();

        public DeclarationTable Parent { get; set; }

        public void Put(string name, PParser.TypeDefDeclContext tree)
        {
            var typedef = new TypeDef(name, tree);
            CheckConflicts(
                typedef,
                Namespace(typedefs),
                Namespace(enums),
                Namespace(interfaces),
                Namespace(machines),
                Namespace(machineProtos));
            typedefs.Add(name, typedef);
        }

        public void Put(string name, PParser.EnumTypeDefDeclContext tree)
        {
            var @enum = new PEnum(name, tree);
            CheckConflicts(
                @enum,
                Namespace(enums),
                Namespace(interfaces),
                Namespace(typedefs),
                Namespace(machines),
                Namespace(machineProtos));
            enums.Add(name, @enum);
        }

        public void Put(string name, PParser.EventDeclContext tree)
        {
            var @event = new PEvent(name, tree);
            CheckConflicts(@event, Namespace(events), Namespace(enumElems));
            events.Add(name, @event);
        }

        public void Put(string name, PParser.EventSetDeclContext tree)
        {
            var eventSet = new EventSet(name, tree);
            CheckConflicts(eventSet, Namespace(eventSets));
            eventSets.Add(name, eventSet);
        }

        public void Put(string name, PParser.InterfaceDeclContext tree)
        {
            var machineInterface = new Interface(name, tree);
            CheckConflicts(
                machineInterface,
                Namespace(interfaces),
                Namespace(enums),
                Namespace(typedefs),
                Namespace(machines),
                Namespace(machineProtos));
            interfaces.Add(name, machineInterface);
        }

        public void Put(string name, PParser.ImplMachineDeclContext tree)
        {
            var machine = new Machine(name, tree);
            CheckConflicts(machine, Namespace(machines), Namespace(interfaces), Namespace(enums), Namespace(typedefs));
            machines.Add(name, machine);
        }

        public void Put(string name, PParser.ImplMachineProtoDeclContext tree)
        {
            var machineProto = new MachineProto(name, tree);
            CheckConflicts(machineProto, Namespace(machineProtos), Namespace(interfaces), Namespace(enums), Namespace(typedefs));
            machineProtos.Add(name, machineProto);
        }

        public void Put(string name, PParser.SpecMachineDeclContext tree)
        {
            var specMachine = new Machine(name, tree);
            CheckConflicts(specMachine, Namespace(machines), Namespace(interfaces), Namespace(enums), Namespace(typedefs));
            machines.Add(name, specMachine);
        }

        public void Put(string name, PParser.FunDeclContext tree)
        {
            var function = new Function(name, tree);
            CheckConflicts(function, Namespace(functions));
            functions.Add(name, function);
        }

        public void Put(string name, PParser.FunProtoDeclContext tree)
        {
            var functionProto = new FunctionProto(name, tree);
            CheckConflicts(functionProto, Namespace(functionProtos));
            functionProtos.Add(name, functionProto);
        }

        public void Put(string name, PParser.GroupContext tree)
        {
            var group = new StateGroup(name, tree);
            CheckConflicts(group, Namespace(stateGroups)); // TODO: also states?
            stateGroups.Add(name, group);
        }

        public void Put(string name, PParser.EnumElemContext tree)
        {
            var enumElem = new EnumElem(name, tree);
            CheckConflicts(enumElem, Namespace(enumElems), Namespace(events));
            enumElems.Add(name, enumElem);
        }

        public void Put(string name, PParser.NumberedEnumElemContext tree)
        {
            var enumElem = new EnumElem(name, tree);
            CheckConflicts(enumElem, Namespace(enumElems), Namespace(events));
            enumElems.Add(name, enumElem);
        }

        public void Put(string name, PParser.VarDeclContext tree)
        {
            var variable = new Variable(name, tree);
            CheckConflicts(variable, Namespace(variables));
            variables.Add(name, variable);
        }

        public void Put(string name, PParser.FunParamContext tree)
        {
            var variable = new Variable(name, tree);
            CheckConflicts(variable, Namespace(variables));
            variables.Add(name, variable);
        }

        public void Put(string name, PParser.StateDeclContext tree)
        {
            var state = new State(name, tree);
            CheckConflicts(state, Namespace(states));
            states.Add(name, state);
        }

        #region Conflict API
        // TODO: maybe optimize this?
        private delegate bool TableReader(string name, out IPDecl decl);

        private static void CheckConflicts(IPDecl decl, params TableReader[] namespaces)
        {
            IPDecl existingDecl = null;
            if (namespaces.Any(table => table(decl.Name, out existingDecl)))
            {
                throw new DuplicateDeclarationException(decl, existingDecl);
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

    public class Variable : IPDecl
    {
        public Variable(string name, PParser.VarDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public Variable(string name, PParser.FunParamContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class EnumElem : IPDecl
    {
        public EnumElem(string name, PParser.EnumElemContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public EnumElem(string name, PParser.NumberedEnumElemContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class StateGroup : IPDecl
    {
        public StateGroup(string name, PParser.GroupContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class State : IPDecl
    {
        public State(string name, PParser.StateDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class MachineProto : IPDecl
    {
        public MachineProto(string name, PParser.ImplMachineProtoDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class Machine : IPDecl
    {
        public Machine(string name, PParser.ImplMachineDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
            IsSpec = false;
        }

        public Machine(string name, PParser.SpecMachineDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
            IsSpec = true;
        }

        public bool IsSpec { get; }
        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class Interface : IPDecl
    {
        public Interface(string name, PParser.InterfaceDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class TypeDef : IPDecl
    {
        public TypeDef(string name, PParser.TypeDefDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class PEnum : IPDecl
    {
        public PEnum(string name, PParser.EnumTypeDefDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public interface IPDecl
    {
        string Name { get; }
        ParserRuleContext SourceNode { get; }
    }

    public class EventSet : IPDecl
    {
        public EventSet(string name, PParser.EventSetDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class PEvent : IPDecl
    {
        public PEvent(string name, PParser.EventDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class Function : IPDecl
    {
        public Function(string name, PParser.FunDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class FunctionProto : IPDecl
    {
        public FunctionProto(string name, PParser.FunProtoDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }
}