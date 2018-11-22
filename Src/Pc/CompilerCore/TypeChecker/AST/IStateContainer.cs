using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.States;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IStateContainer : IPDecl
    {
        IStateContainer ParentStateContainer { get; }
        IEnumerable<State> States { get; }
        IEnumerable<StateGroup> Groups { get; }
        void AddState(State state);
        void AddGroup(StateGroup group);
        IStateContainer GetGroup(string groupName);
        State GetState(string stateName);
    }
}