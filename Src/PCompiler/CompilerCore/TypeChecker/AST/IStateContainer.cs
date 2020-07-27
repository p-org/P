using Plang.Compiler.TypeChecker.AST.States;
using System.Collections.Generic;

namespace Plang.Compiler.TypeChecker.AST
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