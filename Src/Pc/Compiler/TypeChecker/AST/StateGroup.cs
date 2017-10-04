using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class StateGroup : IStateContainer, IHasScope
    {
        private readonly Dictionary<string, StateGroup> _groups = new Dictionary<string, StateGroup>();
        private readonly Dictionary<string, State> _states = new Dictionary<string, State>();

        public StateGroup(string name, PParser.GroupContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public Machine OwningMachine { get; set; }
        public Scope Table { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public IStateContainer ParentStateContainer { get; set; }
        public IEnumerable<State> States => _states.Values;
        public IEnumerable<StateGroup> Groups => _groups.Values;

        public void AddState(State state)
        {
            Debug.Assert(state.Container == null);
            state.Container = this;
            _states.Add(state.Name, state);
        }

        public void AddGroup(StateGroup group)
        {
            Debug.Assert(group.ParentStateContainer == null);
            group.ParentStateContainer = this;
            _groups.Add(group.Name, group);
        }

        public IStateContainer GetGroup(string groupName)
        {
            return _groups.TryGetValue(groupName, out StateGroup group) ? group : null;
        }

        public State GetState(string stateName)
        {
            return _states.TryGetValue(stateName, out State state) ? state : null;
        }
    }
}
