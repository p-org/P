using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class Machine : IConstructibleDecl, IStateContainer, IHasScope
    {
        private readonly Dictionary<string, StateGroup> _groups = new Dictionary<string, StateGroup>();
        private readonly Dictionary<string, State> _states = new Dictionary<string, State>();

        public Machine(string name, ParserRuleContext sourceNode)
        {
            Debug.Assert(sourceNode is PParser.ImplMachineDeclContext || sourceNode is PParser.SpecMachineDeclContext);
            Name = name;
            SourceLocation = sourceNode;
            IsSpec = sourceNode is PParser.SpecMachineDeclContext;
        }
        
        public bool IsSpec { get; }
        public int Assume { get; set; } = -1;
        public int Assert { get; set; } = -1;
        public List<Interface> Interfaces { get; } = new List<Interface>();
        public EventSet Receives { get; set; }
        public EventSet Sends { get; set; }
        public List<Variable> Fields { get; } = new List<Variable>();
        public List<Function> Methods { get; } = new List<Function>();

        public State StartState { get; set; }

        public EventSet Observes { get; set; }
        public string Name { get; }
        public ParserRuleContext SourceLocation { get; }
        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;

        public Scope Table { get; set; }
        public IStateContainer ParentStateContainer { get; } = null;
        public IEnumerable<State> States => _states.Values;
        public IEnumerable<StateGroup> Groups => _groups.Values;

        public IList<IPAST> Children => throw new NotImplementedException("ast children");


        public IStateContainer GetGroup(string groupName)
        {
            return _groups.TryGetValue(groupName, out StateGroup group) ? group : null;
        }

        public State GetState(string stateName)
        {
            return _states.TryGetValue(stateName, out State state) ? state : null;
        }

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
    }
}
