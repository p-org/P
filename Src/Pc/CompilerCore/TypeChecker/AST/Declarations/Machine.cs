using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class Machine : IStateContainer, IHasScope, IPDecl
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
        public ParserRuleContext SourceLocation { get; }

        public Scope Scope { get; set; }
        public string Name { get; }
        public IStateContainer ParentStateContainer { get; } = null;
        public IEnumerable<State> States => states.Values;
        public IEnumerable<StateGroup> Groups => groups.Values;

        public IEnumerable<State> AllStates()
        {
            if (StartState != null)
            {
                yield return StartState;
            }

            var containers = new Stack<IStateContainer>();
            containers.Push(this);
            while (containers.Any())
            {
                var container = containers.Pop();
                foreach (State state in container.States)
                {
                    if (!state.IsStart)
                    {
                        yield return state;
                    }
                }
                foreach (var group in container.Groups)
                {
                    containers.Push(group);
                }
            }
        }

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
            states.Add(state.Name, state);
        }

        public void AddGroup(StateGroup group)
        {
            Debug.Assert(group.ParentStateContainer == null);
            group.ParentStateContainer = this;
            groups.Add(group.Name, group);
        }

        public void AddField(Variable field) { fields.Add(field); }

        public void AddMethod(Function method)
        {
            methods.Add(method);
            method.Owner = this;
            method.Role |= FunctionRole.Method;
        }

        public void AddFields(IEnumerable<Variable> variables) { fields.AddRange(variables); }
    }
}
