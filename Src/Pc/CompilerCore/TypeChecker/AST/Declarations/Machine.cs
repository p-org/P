using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class Machine : IStateContainer, IHasScope
    {
        private readonly List<Variable> fields = new List<Variable>();
        private readonly Dictionary<string, StateGroup> groups = new Dictionary<string, StateGroup>();
        private readonly List<Function> methods = new List<Function>();
        private readonly Dictionary<string, State> states = new Dictionary<string, State>();

        public Machine(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.ImplMachineDeclContext || sourceNode is PParser.SpecMachineDeclContext);
            Name = name;
            SourceLocation = sourceNode;
            IsSpec = sourceNode is PParser.SpecMachineDeclContext;
        }

        public bool IsSpec { get; }
        public uint? Assume { get; set; }
        public uint? Assert { get; set; }
        public IEventSet Receives { get; set; }
        public IEventSet Sends { get; set; }
        public IInterfaceSet Creates { get; set; }
        public IEnumerable<Variable> Fields => fields;
        public IEnumerable<Function> Methods => methods;
        public State StartState { get; set; }
        public IEventSet Observes { get; set; }
        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;

        public Scope Scope { get; set; }
        public ParserRuleContext SourceLocation { get; }
        public string Name { get; }
        public IStateContainer ParentStateContainer { get; } = null;
        public IEnumerable<State> States => states.Values;
        public IEnumerable<StateGroup> Groups => groups.Values;

        public IStateContainer GetGroup(string groupName)
        {
            return groups.TryGetValue(groupName, out StateGroup group) ? group : null;
        }

        public State GetState(string stateName)
        {
            return states.TryGetValue(stateName, out State state) ? state : null;
        }

        public void AddState(State state)
        {
            Debug.Assert(state.Container == null);
            state.Container = this;
            state.OwningMachine = this;
            states.Add(state.Name, state);
        }

        public void AddGroup(StateGroup group)
        {
            Debug.Assert(group.ParentStateContainer == null);
            group.ParentStateContainer = this;
            group.OwningMachine = this;
            groups.Add(group.Name, group);
        }

        public IEnumerable<State> AllStates()
        {
            if (StartState != null)
            {
                yield return StartState;
            }

            Stack<IStateContainer> containers = new Stack<IStateContainer>();
            containers.Push(this);
            while (containers.Any())
            {
                IStateContainer container = containers.Pop();
                foreach (State state in container.States)
                {
                    if (!state.IsStart)
                    {
                        yield return state;
                    }
                }

                foreach (StateGroup group in container.Groups)
                {
                    containers.Push(group);
                }
            }
        }

        public void AddField(Variable field)
        {
            fields.Add(field);
        }

        public void AddMethod(Function method)
        {
            methods.Add(method);
            method.Owner = this;
            method.Role |= FunctionRole.Method;
        }

        public void AddFields(IEnumerable<Variable> variables)
        {
            fields.AddRange(variables);
        }
    }
}