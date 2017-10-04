using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.TypeChecker
{
    public class Validator
    {
        private readonly ParseTreeProperty<IPDecl> _nodesToDeclarations;

        private Validator(ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            _nodesToDeclarations = nodesToDeclarations;
        }

        private bool IsValid(EnumElem enumElem)
        {
            // every enum element should be found among its parent's elements
            // and the map should point to the correct declaration
            return enumElem.ParentEnum.Values.Contains(enumElem) &&
                   _nodesToDeclarations.Get(enumElem.SourceNode) == enumElem;
        }

        private bool IsValid(EventSet eventSet) { return _nodesToDeclarations.Get(eventSet.SourceNode) == eventSet; }

        private bool IsValid(Function function)
        {
            return function.Owner?.Methods.Contains(function) != false && // function properly registered with machine
                   function.Signature.ReturnType != null && // function signature has return type
                   function.Signature.Parameters.All(param => param.Type != null) && // function signature parameters have types
                   _nodesToDeclarations.Get(function.SourceNode) == function; // map is bi-directional
        }

        private bool IsValid(FunctionProto functionProto)
        {
            return functionProto.Signature.ReturnType != null && // function proto has return type
                   functionProto.Signature.Parameters
                                .All(p => p.Type != null) && // function parameters have known types
                   _nodesToDeclarations.Get(functionProto.SourceNode) == functionProto;
        }

        private bool IsValid(Interface pInterface)
        {
            return pInterface.PayloadType != null && // interface has known payload type
                   _nodesToDeclarations.Get(pInterface.SourceNode) == pInterface;
        }

        private static IEnumerable<State> Flatten(IEnumerable<StateGroup> groups)
        {
            foreach (StateGroup group in groups)
            {
                foreach (State groupState in group.States)
                {
                    yield return groupState;
                }

                foreach (State subState in Flatten(group.Groups))
                {
                    yield return subState;
                }
            }
        }

        private bool IsValid(Machine machine)
        {
            var allStates = machine.States.Concat(Flatten(machine.Groups)).ToList();
            bool success = machine.Methods.All(fun => fun.Owner == machine);
            success &= machine.PayloadType != null;
            success &= machine.StartState != null;
            success &= allStates.Contains(machine.StartState);
            success &= allStates.All(st => !st.IsStart || st.IsStart && st == machine.StartState);
            success &= machine.Fields.All(v => v.IsParam == false);
            success &= _nodesToDeclarations.Get(machine.SourceNode) == machine;
            return success;
        }

        private bool IsValid(MachineProto machineProto)
        {
            return machineProto.PayloadType != null &&
                   _nodesToDeclarations.Get(machineProto.SourceNode) == machineProto;
        }

        private bool IsValid(PEnum pEnum)
        {
            // All of its values have the correct parent registered
            // There is a zero element
            // All elements are distinct
            return pEnum.Values.All(val => val.ParentEnum == pEnum) &&
                   pEnum.Values.Any(val => val.Value == 0) &&
                   pEnum.Values.Select(val => val.Value).Distinct().Count() == pEnum.Values.Count() &&
                   _nodesToDeclarations.Get(pEnum.SourceNode) == pEnum;
        }

        private bool IsValid(PEvent pEvent)
        {
            // special handling for special events
            if (pEvent.SourceNode == null)
            {
                return pEvent.Name.Equals("halt") || pEvent.Name.Equals("null");
            }

            // check that reverse trips works
            return _nodesToDeclarations.Get(pEvent.SourceNode) == pEvent;
        }

        private bool IsValid(State state)
        {
            return state.Container.States.Contains(state) &&
                   state.Actions.All(kv => kv.Value.Trigger == kv.Key) &&
                   _nodesToDeclarations.Get(state.SourceNode) == state;
        }

        private bool IsValid(StateGroup stateGroup)
        {
            return stateGroup.ParentStateContainer.Groups.Contains(stateGroup) &&
                   stateGroup.States.All(state => state.Container == stateGroup) &&
                   stateGroup.States.All(state => state.OwningMachine == stateGroup.OwningMachine) &&
                   stateGroup.Groups.All(group => group.OwningMachine == stateGroup.OwningMachine) &&
                   stateGroup.Groups.All(group => group.ParentStateContainer == stateGroup) &&
                   _nodesToDeclarations.Get(stateGroup.SourceNode) == stateGroup;
        }

        private bool IsValid(TypeDef typeDef)
        {
            return typeDef.Type != null &&
                   _nodesToDeclarations.Get(typeDef.SourceNode) == typeDef;
        }

        private bool IsValid(Variable variable)
        {
            return variable.Type != null &&
                   _nodesToDeclarations.Get(variable.SourceNode) == variable;
        }

        [Conditional("DEBUG")]
        public static void ValidateDeclarations(
            ParseTreeProperty<IPDecl> nodesToDeclarations,
            Scope topLevelTable)
        {
            var validator = new Validator(nodesToDeclarations);
            foreach (IPDecl decl in AllDeclarations(topLevelTable))
            {
                if (!validator.IsValid((dynamic) decl))
                {
                    throw new ArgumentException($"malformed declaration {decl.Name}");
                }
            }
        }

        private static IEnumerable<IPDecl> AllDeclarations(Scope root)
        {
            foreach (IPDecl decl in root.AllDecls)
            {
                yield return decl;
            }
            foreach (Scope child in root.Children)
            {
                foreach (IPDecl subdecl in AllDeclarations(child))
                {
                    yield return subdecl;
                }
            }
        }
    }
}
