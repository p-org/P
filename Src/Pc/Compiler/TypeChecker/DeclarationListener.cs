using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclarationListener : PParserBaseListener
    {
        public DeclarationListener(
            ITranslationErrorHandler handler,
            ParseTreeProperty<Scope> nodesToScopes,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.handler = handler;
            this.nodesToScopes = nodesToScopes;
            this.nodesToDeclarations = nodesToDeclarations;
        }

        #region Events

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            // EVENT name=Iden
            var pEvent = (PEvent) nodesToDeclarations.Get(context);

            // cardinality?
            bool hasAssume = context.cardinality()?.ASSUME() != null;
            bool hasAssert = context.cardinality()?.ASSERT() != null;
            int cardinality = int.Parse(context.cardinality()?.IntLiteral().GetText() ?? "-1");
            pEvent.Assume = hasAssume ? cardinality : -1;
            pEvent.Assert = hasAssert ? cardinality : -1;

            // (COLON type)?
            pEvent.PayloadType = TypeResolver.ResolveType(context.type(), currentScope, handler);

            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("event annotations");
            }

            // SEMI ;
        }

        #endregion

        #region Interfaces

        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            // TYPE name=Iden
            var mInterface = (Interface) nodesToDeclarations.Get(context);

            // LPAREN type? RPAREN
            mInterface.PayloadType = TypeResolver.ResolveType(context.type(), currentScope, handler);

            if (context.eventSet == null)
            {
                // ASSIGN LBRACE eventSetLiteral RBRACE
                // Let the eventSetLiteral handler fill in a newly created event set...
                PParser.EventSetLiteralContext eventSetLiteral = context.eventSetLiteral();
                Debug.Assert(eventSetLiteral != null);
                mInterface.ReceivableEvents = new EventSet($"{mInterface.Name}$eventset", eventSetLiteral);
            }
            else
            {
                // ASSIGN eventSet=Iden
                // ...or look up the event set and establish the link by name.
                string eventSetName = context.eventSet.GetText();
                if (!currentScope.Lookup(eventSetName, out EventSet eventSet))
                {
                    throw handler.MissingDeclaration(context.eventSet, "event set", eventSetName);
                }

                mInterface.ReceivableEvents = eventSet;
            }

            currentEventSet = mInterface.ReceivableEvents;
        }

        #endregion

        #region Functions
        public override void EnterPFunDecl(PParser.PFunDeclContext context)
        {
            // FUN name=Iden
            var fun = (Function) nodesToDeclarations.Get(context);
            currentMachine?.Methods.Add(fun);
            fun.Owner = currentMachine;

            // LPAREN funParamList? RPAREN
            functionStack.Push(fun); // funParamList builds signature

            // (COLON type)?
            fun.Signature.ReturnType = TypeResolver.ResolveType(context.type(), currentScope, handler);

            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("function annotations");
            }
            
            // functionBody handled in EnterFunctionBody
        }

        public override void ExitPFunDecl(PParser.PFunDeclContext context) { functionStack.Pop(); }

        public override void EnterForeignFunDecl(PParser.ForeignFunDeclContext context)
        {
            throw new NotImplementedException("foreign functions");
        }

        public override void EnterFunParam(PParser.FunParamContext context)
        {
            // name=Iden
            string name = context.name.GetText();
            // COLON type ;
            PLanguageType type = TypeResolver.ResolveType(context.type(), currentScope, handler);

            ITypedName param;
            if (currentFunctionProto != null)
            {
                // If we're in a prototype, then we don't look up a variable, we just create a formal parameter
                param = new FormalParameter
                {
                    Name = name,
                    Type = type
                };
            }
            else
            {
                // Otherwise, we're in a (possibly anonymous) function, and we add the variable to its signature
                bool success = currentScope.Get(name, out Variable variable);
                Debug.Assert(success);
                variable.Type = type;
                param = variable;
            }

            CurrentFunction.Signature.Parameters.Add(param);
        }
        #endregion

        public override void EnterVarDecl(PParser.VarDeclContext context)
        {
            // VAR idenList
            var varNames = context.idenList()._names;
            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("variable annotations");
            }

            PLanguageType variableType = TypeResolver.ResolveType(context.type(), currentScope, handler);
            foreach (PParser.IdenContext varName in varNames)
            {
                var variable = (Variable) nodesToDeclarations.Get(varName);
                // COLON type
                variable.Type = variableType;

                if (CurrentFunction != null)
                {
                    CurrentFunction.LocalVariables.Add(variable);
                }
                else
                {
                    Debug.Assert(currentMachine != null);
                    currentMachine.Fields.Add(variable);
                }
            }
            // SEMI
        }

        public override void EnterGroup(PParser.GroupContext context)
        {
            // GROUP name=Iden
            var group = (StateGroup) nodesToDeclarations.Get(context);
            group.OwningMachine = currentMachine;
            // LBRACE groupItem* RBRACE
            CurrentStateContainer.AddGroup(group);
            stateContainerStack.Push(group);
        }

        public override void ExitGroup(PParser.GroupContext context) { stateContainerStack.Pop(); }

        public override void EnterStateDecl(PParser.StateDeclContext context)
        {
            currentState = (State) nodesToDeclarations.Get(context);
            CurrentStateContainer.AddState(currentState);
            currentState.OwningMachine = currentMachine;

            // START?
            currentState.IsStart = context.START() != null;
            if (currentState.IsStart)
            {
                if (currentMachine.StartState != null)
                {
                    throw handler.DuplicateStartState(context, currentState, currentMachine.StartState, currentMachine);
                }
                currentMachine.StartState = currentState;
            }

            // temperature=(HOT | COLD)?
            currentState.Temperature = context.temperature == null
                                           ? StateTemperature.WARM
                                           : context.temperature.Text.Equals("HOT")
                                               ? StateTemperature.HOT
                                               : StateTemperature.COLD;

            // STATE name=Iden
            // handled above with lookup.

            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("state annotations");
            }

            // LBRACE stateBodyItem* RBRACE
            // handled by StateEntry / StateExit / StateDefer / StateIgnore / OnEventDoAction / OnEventPushState / OnEventGotoState
        }

        public override void ExitStateDecl(PParser.StateDeclContext context)
        {
            if (currentState.IsStart)
            {
                // The machine's payload type is the start state's entry payload type (or null, by default)
                currentMachine.PayloadType = PrimitiveType.Null;
                if (currentState.Entry?.Signature.Parameters.Count > 0)
                {
                    currentMachine.PayloadType = currentState.Entry.Signature.Parameters[0].Type;
                }
            }
            currentState = null;
        }

        public override void EnterStateEntry(PParser.StateEntryContext context)
        {
            // (
            Function fun;

            if (context.anonEventHandler() != null)
            {
                // ENTRY anonEventHandler 
                fun = new Function(context.anonEventHandler()) {Owner = currentMachine};
                nodesToDeclarations.Put(context.anonEventHandler(), fun);
            }
            else // |
            {
                // ENTRY funName=Iden)
                string funName = context.funName.GetText();
                if (!currentScope.Lookup(funName, out fun))
                {
                    if (currentScope.Lookup(funName, out FunctionProto proto))
                    {
                        throw new NotImplementedException("function prototypes for state entries");
                    }
                    throw handler.MissingDeclaration(context.funName, "function", funName);
                }
            }
            // SEMI
            if (currentState.Entry != null)
            {
                throw handler.DuplicateStateEntry(context, currentState.Entry, currentState);
            }
            currentState.Entry = fun;
            functionStack.Push(fun);
        }

        public override void ExitStateEntry(PParser.StateEntryContext context) { functionStack.Pop(); }

        public override void EnterOnEventDoAction(PParser.OnEventDoActionContext context)
        {
            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("state action annotations");
            }

            Function fun;
            if (context.anonEventHandler() != null)
            {
                // DO [...] anonEventHandler
                fun = new Function(context.anonEventHandler()) {Owner = currentMachine};
                nodesToDeclarations.Put(context.anonEventHandler(), fun);
            }
            else
            {
                // DO funName=Iden
                string funName = context.funName.GetText();
                if (!currentScope.Lookup(funName, out fun))
                {
                    if (currentScope.Lookup(funName, out FunctionProto proto))
                    {
                        throw new NotImplementedException("function prototypes for state actions");
                    }
                    throw handler.MissingDeclaration(context.funName, "function", funName);
                }
            }

            // ON eventList
            foreach (PParser.EventIdContext eventIdContext in context.eventList().eventId())
            {
                if (!currentScope.Lookup(eventIdContext.GetText(), out PEvent evt))
                {
                    throw handler.MissingDeclaration(eventIdContext, "event", eventIdContext.GetText());
                }

                if (currentState.Actions.ContainsKey(evt))
                {
                    throw handler.DuplicateEventAction(eventIdContext, currentState.Actions[evt], currentState);
                }

                currentState.Actions.Add(evt, new EventDoAction(evt, fun));
            }

            // SEMI
            functionStack.Push(fun);
        }

        public override void ExitOnEventDoAction(PParser.OnEventDoActionContext context) { functionStack.Pop(); }

        public override void EnterStateExit(PParser.StateExitContext context)
        {
            // EXIT
            Function fun;
            if (context.noParamAnonEventHandler() != null)
            {
                // noParamAnonEventHandler
                fun = new Function(context.noParamAnonEventHandler()) {Owner = currentMachine};
                nodesToDeclarations.Put(context.noParamAnonEventHandler(), fun);
            }
            else
            {
                // funName=Iden
                string funName = context.funName.GetText();
                if (!currentScope.Lookup(funName, out fun))
                {
                    if (currentScope.Lookup(funName, out FunctionProto proto))
                    {
                        throw new NotImplementedException("function prototypes for state exits");
                    }
                    throw handler.MissingDeclaration(context.funName, "function", funName);
                }
            }
            // SEMI
            if (currentState.Exit != null)
            {
                throw handler.DuplicateStateExitHandler(context, currentState.Exit, currentState);
            }

            currentState.Exit = fun;
            functionStack.Push(fun);
        }

        public override void ExitStateExit(PParser.StateExitContext context) { functionStack.Pop(); }

        public override void EnterOnEventGotoState(PParser.OnEventGotoStateContext context)
        {
            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("state transition annotations");
            }

            Function transitionFunction;
            if (context.funName != null)
            {
                // WITH funName=Iden
                string funName = context.funName.GetText();
                if (!currentScope.Lookup(funName, out transitionFunction))
                {
                    throw handler.MissingDeclaration(context.funName, "function", funName);
                }
            }
            else if (context.anonEventHandler() != null)
            {
                // WITH anonEventHandler
                transitionFunction = new Function(context.anonEventHandler()) {Owner = currentMachine};
                nodesToDeclarations.Put(context.anonEventHandler(), transitionFunction);
            }
            else
            {
                // SEMI
                transitionFunction = null;
            }
            functionStack.Push(transitionFunction);

            // GOTO stateName 
            State target = FindState(context.stateName());

            // ON eventList
            foreach (PParser.EventIdContext eventIdContext in context.eventList().eventId())
            {
                if (!currentScope.Lookup(eventIdContext.GetText(), out PEvent evt))
                {
                    throw handler.MissingDeclaration(eventIdContext, "event", eventIdContext.GetText());
                }

                if (currentState.Actions.ContainsKey(evt))
                {
                    throw handler.DuplicateEventAction(eventIdContext, currentState.Actions[evt], currentState);
                }

                currentState.Actions.Add(evt, new EventGotoState(evt, target, transitionFunction));
            }
        }

        public override void ExitOnEventGotoState(PParser.OnEventGotoStateContext context) { functionStack.Pop(); }

        public override void EnterStateIgnore(PParser.StateIgnoreContext context)
        {
            // annotationSet? 
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("event ignore annotations");
            }
            // IGNORE nonDefaultEventList
            foreach (PParser.NonDefaultEventContext token in context.nonDefaultEventList()._events)
            {
                if (!currentScope.Lookup(token.GetText(), out PEvent evt))
                {
                    throw handler.MissingDeclaration(token, "event", token.GetText());
                }
                if (currentState.Actions.ContainsKey(evt))
                {
                    throw handler.DuplicateEventAction(token, currentState.Actions[evt], currentState);
                }

                currentState.Actions.Add(evt, new EventIgnore(evt));
            }
        }

        public override void EnterStateDefer(PParser.StateDeferContext context)
        {
            // annotationSet? SEMI
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("event defer annotations");
            }
            // DEFER nonDefaultEventList 
            foreach (PParser.NonDefaultEventContext token in context.nonDefaultEventList()._events)
            {
                if (!currentScope.Lookup(token.GetText(), out PEvent evt))
                {
                    throw handler.MissingDeclaration(token, "event", token.GetText());
                }
                if (currentState.Actions.ContainsKey(evt))
                {
                    throw handler.DuplicateEventAction(token, currentState.Actions[evt], currentState);
                }
                currentState.Actions.Add(evt, new EventDefer(evt));
            }
        }

        public override void EnterOnEventPushState(PParser.OnEventPushStateContext context)
        {
            //annotationSet? 
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("push state annotations");
            }

            // PUSH stateName 
            State targetState = FindState(context.stateName());

            // ON eventList
            foreach (PParser.EventIdContext token in context.eventList().eventId())
            {
                if (!currentScope.Lookup(token.GetText(), out PEvent evt))
                {
                    throw handler.MissingDeclaration(token, "event", token.GetText());
                }
                if (currentState.Actions.ContainsKey(evt))
                {
                    throw handler.DuplicateEventAction(token, currentState.Actions[evt], currentState);
                }

                currentState.Actions.Add(evt, new EventPushState(evt, targetState));
            }
        }

        private State FindState(PParser.StateNameContext context)
        {
            Scope curTable = nodesToScopes.Get(currentMachine.SourceLocation);
            foreach (PParser.IdenContext groupToken in context._groups)
            {
                if (!curTable.Get(groupToken.GetText(), out StateGroup group))
                {
                    throw handler.MissingDeclaration(groupToken, "group", groupToken.GetText());
                }
                curTable = nodesToScopes.Get(group.SourceLocation);
            }
            if (!curTable.Get(context.state.GetText(), out State state))
            {
                throw handler.MissingDeclaration(context.state, "state", context.state.GetText());
            }
            return state;
        }
        
        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            // SPEC name=Iden 
            var specMachine = (Machine) nodesToDeclarations.Get(context);
            stateContainerStack.Push(specMachine);
            // OBSERVES eventSetLiteral
            specMachine.Observes = new EventSet($"{specMachine.Name}$eventset", context.eventSetLiteral());
            currentEventSet = specMachine.Observes;
            // machineBody
            currentMachine = specMachine;
        }

        public override void ExitSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            currentEventSet = null;
            currentMachine = null;
            stateContainerStack.Pop();
            var specMachine = (Machine) nodesToDeclarations.Get(context);
            if (specMachine.StartState == null)
            {
                throw new NotImplementedException("machines with no start state");
            }
        }

        public override void EnterEveryRule(ParserRuleContext ctx)
        {
            Scope thisTable = nodesToScopes.Get(ctx);
            if (thisTable != null)
            {
                currentScope = thisTable;
            }
        }

        public override void ExitEveryRule(ParserRuleContext context)
        {
            if (nodesToScopes.Get(context) != null)
            {
                Debug.Assert(currentScope != null);
                // pop the stack
                currentScope = currentScope.Parent;
            }
        }

        public override void ExitInterfaceDecl(PParser.InterfaceDeclContext context) { currentEventSet = null; }

        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            // eventDecl : MACHINE name=Iden
            currentMachine = (Machine) nodesToDeclarations.Get(context);
            stateContainerStack.Push(currentMachine);

            // cardinality?
            bool hasAssume = context.cardinality()?.ASSUME() != null;
            bool hasAssert = context.cardinality()?.ASSERT() != null;
            int cardinality = int.Parse(context.cardinality()?.IntLiteral().GetText() ?? "-1");
            currentMachine.Assume = hasAssume ? cardinality : -1;
            currentMachine.Assert = hasAssert ? cardinality : -1;

            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("machine annotations");
            }

            // (COLON idenList)?
            if (context.idenList() != null)
            {
                var interfaces = context.idenList()._names;
                foreach (PParser.IdenContext pInterfaceNameCtx in interfaces)
                {
                    string pInterfaceName = pInterfaceNameCtx.GetText();
                    if (!currentScope.Lookup(pInterfaceName, out Interface pInterface))
                    {
                        throw handler.MissingDeclaration(pInterfaceNameCtx, "interface", pInterfaceName);
                    }

                    pInterface.Implementations.Add(currentMachine);
                    currentMachine.Interfaces.Add(pInterface);
                }
            }

            // receivesSends*
            // handled by EnterReceivesSends

            // machineBody
            // handled by EnterVarDecl / EnterFunDecl / EnterGroup / EnterStateDecl
        }

        public override void ExitImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            stateContainerStack.Pop();
            currentMachine = null;
            var machine = (Machine) nodesToDeclarations.Get(context);
            if (machine.StartState == null)
            {
                throw new NotImplementedException("machines with no start state");
            }
        }

        public override void EnterMachineReceive(PParser.MachineReceiveContext context)
        {
            // RECEIVES eventSetLiteral? SEMI
            if (currentMachine.Receives == null)
            {
                currentMachine.Receives = new EventSet($"{currentMachine.Name}$receives", context.eventSetLiteral());
            }
            currentEventSet = currentMachine.Receives;
        }

        public override void ExitMachineReceive(PParser.MachineReceiveContext context) { currentEventSet = null; }

        public override void EnterMachineSend(PParser.MachineSendContext context)
        {
            // SENDS eventSetLiteral? SEMI
            if (currentMachine.Sends == null)
            {
                currentMachine.Sends = new EventSet($"{currentMachine.Name}$sends", context.eventSetLiteral());
            }
            currentEventSet = currentMachine.Sends;
        }

        public override void ExitMachineSend(PParser.MachineSendContext context) { currentEventSet = null; }

        #region Parse tree propertoes

        /// <summary>
        ///     Maps source nodes to the unique declarations they produced.
        /// </summary>
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;

        /// <summary>
        /// Handles errors in AST construction and type checking
        /// </summary>
        private readonly ITranslationErrorHandler handler;

        /// <summary>
        ///     Maps source nodes to the scope objects they produced.
        /// </summary>
        private readonly ParseTreeProperty<Scope> nodesToScopes;

        #endregion

        #region Context fields

        /// <summary>
        ///     Functions can be nested via anonymous event handlers, so we do need to keep track.
        /// </summary>
        private readonly Stack<Function> functionStack = new Stack<Function>();

        /// <summary>
        ///     Groups can be nested
        /// </summary>
        private readonly Stack<IStateContainer> stateContainerStack = new Stack<IStateContainer>();

        /// <summary>
        ///     Enum declarations can't be nested, so we simply store the most recently encountered
        ///     one in a variable for the listener actions for the elements to access.
        /// </summary>
        private PEnum currentEnum;

        /// <summary>
        ///     Event sets cannot be nested, so we keep track only of the most recent one.
        /// </summary>
        private EventSet currentEventSet;

        /// <summary>
        ///     Function prototypes cannot be nested, so we keep track only of the most recent one.
        /// </summary>
        private FunctionProto currentFunctionProto;

        /// <summary>
        ///     Machines cannot be nested, so we keep track of only the most recent one.
        /// </summary>
        private Machine currentMachine;

        /// <summary>
        ///     There can't be any nested states, so we only keep track of the most recent.
        /// </summary>
        private State currentState;

        /// <summary>
        ///     This keeps track of the current scope. The "on every entry/exit" rules handle popping the
        ///     stack using its Parent pointer.
        /// </summary>
        private Scope currentScope;

        /// <summary>
        ///     Gets the current function or null if not in a function context.
        /// </summary>
        private Function CurrentFunction => functionStack.Count > 0 ? functionStack.Peek() : null;

        /// <summary>
        ///     Gets the current state group or null if not in a state group context
        /// </summary>
        private IStateContainer CurrentStateContainer =>
            stateContainerStack.Count > 0 ? stateContainerStack.Peek() : null;

        #endregion

        #region Typedefs

        public override void EnterPTypeDef(PParser.PTypeDefContext context)
        {
            // TYPE name=Iden 
            var typedef = (TypeDef) nodesToDeclarations.Get(context);

            // ASSIGN type
            typedef.Type = TypeResolver.ResolveType(context.type(), currentScope, handler);
        }

        public override void EnterForeignTypeDef(PParser.ForeignTypeDefContext context)
        {
            // TYPE name=Iden
            var typedef = (TypeDef)nodesToDeclarations.Get(context);

            // SEMI
            typedef.Type = TypeResolver.ResolveType(context, currentScope, handler);
        }

        #endregion

        #region Enums

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            // ENUM name=Iden LBRACE enumElemList RBRACE | ENUM name = Iden LBRACE numberedEnumElemList RBRACE
            currentEnum = (PEnum) nodesToDeclarations.Get(context);
        }

        public override void ExitEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context) { currentEnum = null; }

        public override void EnterEnumElem(PParser.EnumElemContext context)
        {
            // name=Iden
            var elem = (EnumElem) nodesToDeclarations.Get(context);
            elem.Value = currentEnum.Count; // listener visits from left-to-right, so this will count upwards correctly.
            bool success = currentEnum.AddElement(elem);
            Debug.Assert(success, "automatic numbering of enum elements failed");
        }

        public override void EnterNumberedEnumElem(PParser.NumberedEnumElemContext context)
        {
            // name=Iden 
            var elem = (EnumElem) nodesToDeclarations.Get(context);
            // ASSIGN value=IntLiteral
            elem.Value = int.Parse(context.value.Text);
            if (!currentEnum.AddElement(elem))
            {
                throw handler.DuplicateEnumValue(context, currentEnum);
            }
        }

        #endregion

        #region Event sets

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            // EVENTSET name=Iden ASSIGN LBRACE eventSetLiteral RBRACE SEMI ;
            currentEventSet = (EventSet) nodesToDeclarations.Get(context);
        }

        public override void ExitEventSetDecl(PParser.EventSetDeclContext context) { currentEventSet = null; }

        public override void EnterEventSetLiteral(PParser.EventSetLiteralContext context)
        {
            // events+=(HALT | Iden) (COMMA events+=(HALT | Iden))* ;
            foreach (PParser.NonDefaultEventContext token in context._events)
            {
                string eventName = token.GetText();
                if (!currentScope.Lookup(eventName, out PEvent evt))
                {
                    throw handler.MissingDeclaration(token, "event", eventName);
                }

                currentEventSet.Events.Add(evt);
            }
        }

        #endregion
    }
}
