using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.States;

namespace Plang.Compiler.TypeChecker.AST
{
    public interface IStateContainer : IPDecl
    {
        IStateContainer ParentStateContainer { get; }
        IEnumerable<State> States { get; }

        void AddState(State state);

        State GetState(string stateName);
    }
}