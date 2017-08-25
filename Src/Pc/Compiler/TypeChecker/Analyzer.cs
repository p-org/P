using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public static class Analyzer
    {
        public static void AnalyzeCompilationUnit(ITranslationErrorHandler handler, params PParser.ProgramContext[] programUnits)
        {
            var walker = new ParseTreeWalker();
            var topLevelTable = new DeclarationTable(handler);
            var nodesToScopes = new ParseTreeProperty<DeclarationTable>();
            var nodesToDeclarations = new ParseTreeProperty<IPDecl>();
            var stubListener = new DeclarationStubListener(topLevelTable, nodesToScopes, nodesToDeclarations, handler);
            var declListener = new DeclarationListener(handler, nodesToScopes, nodesToDeclarations);

            // Add built-in events to the table.
            topLevelTable.Put("halt", (PParser.EventDeclContext) null);
            topLevelTable.Put("null", (PParser.EventDeclContext) null);

            // Step 1: Create mapping of names to declaration stubs
            foreach (PParser.ProgramContext programUnit in programUnits)
                walker.Walk(stubListener, programUnit);

            // NOW: no declarations have ambiguous names.
            // NOW: there is exactly one declaration object for each declaration.
            // NOW: every declaration object is associated in both directions with its corresponding parse tree node.
            // NOW: enums and their elements are related to one another

            // Step 2: Validate declarations and fill with types
            foreach (PParser.ProgramContext programUnit in programUnits)
                walker.Walk(declListener, programUnit);

            ValidateDeclarations(nodesToScopes, nodesToDeclarations, topLevelTable);

            // NOW: all declarations are valid, with appropriate links and types resolved.

            // Step 3: Fill in method bodies
            foreach (var declaration in AllDeclarations(topLevelTable))
            {
                if (!(declaration.Item1 is Function fun))
                {
                    continue;
                }

                PParser.FunctionBodyContext functionBody;
                DeclarationTable table = nodesToScopes.Get(fun.SourceNode);
                if (fun.SourceNode is PParser.FunDeclContext funDecl)
                {
                    functionBody = funDecl.functionBody();
                }
                else if (fun.SourceNode is PParser.AnonEventHandlerContext anonHandler)
                {
                    functionBody = anonHandler.functionBody();
                }
                else if (fun.SourceNode is PParser.NoParamAnonEventHandlerContext noParamAnonHandler)
                {
                    functionBody = noParamAnonHandler.functionBody();
                }
                else
                {
                    throw new ArgumentException();
                }
                var statementVisitor = new StatementVisitor(table, fun.Owner, handler);
                fun.Body = functionBody.statement().SelectMany(stmt => statementVisitor.Visit(stmt)).ToList();
            }

            // NOW: AST Complete, pass to StringTemplate
        }

        [Conditional("DEBUG")]
        private static void ValidateDeclarations(
            ParseTreeProperty<DeclarationTable> programDeclarations,
            ParseTreeProperty<IPDecl> nodesToDeclarations,
            DeclarationTable topLevelTable)
        {
            var validator = new Validator(programDeclarations, nodesToDeclarations);
            foreach (var decl in AllDeclarations(topLevelTable))
            {
                if (!validator.IsValid((dynamic) decl.Item1, decl.Item2))
                    throw new ArgumentException($"malformed declaration {decl.Item1.Name}");
            }
        }

        private static IEnumerable<Tuple<IPDecl, DeclarationTable>> AllDeclarations(DeclarationTable root)
        {
            foreach (IPDecl decl in root.AllDecls)
                yield return Tuple.Create(decl, root);
            foreach (DeclarationTable child in root.Children)
            {
                foreach (var subdecl in AllDeclarations(child))
                    yield return subdecl;
            }
        }
    }

    public class Validator
    {
        private readonly ParseTreeProperty<IPDecl> _nodesToDeclarations;
        private readonly ParseTreeProperty<DeclarationTable> _programDeclarations;

        public Validator(
            ParseTreeProperty<DeclarationTable> programDeclarations,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            _programDeclarations = programDeclarations;
            _nodesToDeclarations = nodesToDeclarations;
        }

        public bool IsValid(EnumElem enumElem, DeclarationTable sourceTable)
        {
            // every enum element should be found among its parent's elements
            // and the map should point to the correct declaration
            return enumElem.ParentEnum.Values.Contains(enumElem) &&
                   _nodesToDeclarations.Get(enumElem.SourceNode) == enumElem;
        }

        public bool IsValid(EventSet eventSet, DeclarationTable sourceTable)
        {
            return _nodesToDeclarations.Get(eventSet.SourceNode) == eventSet;
        }

        public bool IsValid(Function function, DeclarationTable sourceTable)
        {
            return function.Owner?.Methods.Contains(function) != false && // function properly registered with machine
                   function.Signature.ReturnType != null && // function signature has return type
                   function.Signature.Parameters.All(
                                                     param => param.Type !=
                                                              null) && // function signature parameters have types
                   _nodesToDeclarations.Get(function.SourceNode) == function; // map is bi-directional
        }

        public bool IsValid(FunctionProto functionProto, DeclarationTable sourceTable)
        {
            return functionProto.Signature.ReturnType != null && // function proto has return type
                   functionProto.Signature.Parameters
                                .All(p => p.Type != null) && // function parameters have known types
                   _nodesToDeclarations.Get(functionProto.SourceNode) == functionProto;
        }

        public bool IsValid(Interface pInterface, DeclarationTable sourceTable)
        {
            return pInterface.PayloadType != null && // interface has known payload type
                   _nodesToDeclarations.Get(pInterface.SourceNode) == pInterface;
        }

        private static IEnumerable<State> Flatten(IEnumerable<StateGroup> groups)
        {
            foreach (StateGroup group in groups)
            {
                foreach (State groupState in group.States)
                    yield return groupState;

                foreach (State subState in Flatten(group.Groups))
                    yield return subState;
            }
        }

        public bool IsValid(Machine machine, DeclarationTable sourceTable)
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

        public bool IsValid(MachineProto machineProto, DeclarationTable sourceTable)
        {
            return machineProto.PayloadType != null &&
                _nodesToDeclarations.Get(machineProto.SourceNode) == machineProto;
        }

        public bool IsValid(PEnum pEnum, DeclarationTable sourceTable)
        {
            // All of its values have the correct parent registered
            // There is a zero element
            // All elements are distinct
            return pEnum.Values.All(val => val.ParentEnum == pEnum) &&
                pEnum.Values.Any(val => val.Value == 0) &&
                pEnum.Values.Select(val => val.Value).Distinct().Count() == pEnum.Values.Count() &&
                _nodesToDeclarations.Get(pEnum.SourceNode) == pEnum;
        }

        public bool IsValid(PEvent pEvent, DeclarationTable sourceTable)
        {
            // special handling for special events
            if (pEvent.SourceNode == null)
                return pEvent.Name.Equals("halt") || pEvent.Name.Equals("null");

            // check that reverse trips works
            return _nodesToDeclarations.Get(pEvent.SourceNode) == pEvent;
        }

        public bool IsValid(State state, DeclarationTable sourceTable)
        {
            return state.Container.States.Contains(state) &&
                state.Actions.All(kv => kv.Value.Trigger == kv.Key) &&
                _nodesToDeclarations.Get(state.SourceNode) == state;
        }

        public bool IsValid(StateGroup stateGroup, DeclarationTable sourceTable)
        {
            return stateGroup.ParentStateContainer.Groups.Contains(stateGroup) &&
                stateGroup.States.All(state => state.Container == stateGroup) &&
                stateGroup.States.All(state => state.OwningMachine == stateGroup.OwningMachine) &&
                stateGroup.Groups.All(group => group.OwningMachine == stateGroup.OwningMachine) &&
                stateGroup.Groups.All(group => group.ParentStateContainer == stateGroup) &&
                _nodesToDeclarations.Get(stateGroup.SourceNode) == stateGroup;
        }

        public bool IsValid(TypeDef typeDef, DeclarationTable sourceTable)
        {
            return typeDef.Type != null &&
                _nodesToDeclarations.Get(typeDef.SourceNode) == typeDef;
        }

        public bool IsValid(Variable variable, DeclarationTable sourceTable)
        {
            return variable.Type != null &&
                _nodesToDeclarations.Get(variable.SourceNode) == variable;
        }
    }
}
