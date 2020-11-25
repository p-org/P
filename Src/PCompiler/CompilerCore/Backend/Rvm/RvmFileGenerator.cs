using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Plang.Compiler.Backend.Rvm
{
    // RvmFileGenerator generates the rvm specification.
    internal class RvmFileGenerator
    {
        private CompilationContext Context {get;}
        private GeneratorTools Tools {get;}
        private Dictionary<string, PEnum> EnumDecls {get;}

        internal RvmFileGenerator(CompilationContext context)
        {
            Context = context;
            Tools = new GeneratorTools(Context);
            EnumDecls = new Dictionary<string, PEnum>();
        }

        public IEnumerable<CompiledFile> GenerateSources(Scope globalScope)
        {
            List<CompiledFile> sources = new List<CompiledFile>();

            foreach (IPDecl decl in globalScope.AllDecls)
            {
                switch (decl)
                {
                    case PEnum pEnum:
                    {
                        EnumDecls.Add(pEnum.Name, pEnum);
                        break;
                    }
                    default:
                        // Just ignore.
                        break;
                }
            }

            int specCount = 0;
            foreach (Machine machine in globalScope.Machines)
            {
                if (machine.IsSpec)
                {
                    if (specCount > 0)
                    {
                         throw new NotImplementedException("More than one spec is not supported.");
                    }
                    sources.Add(WriteMonitor(machine));
                    specCount++;
                }
            }

            return sources;
        }

        private CompiledFile WriteMonitor(Machine machine) {
            CompiledFile source = new CompiledFile(Context.GetRvmFileName(machine));

            WriteSourcePrologue(source.Stream);

            WriteSpec(source.Stream, machine);

            return source;
        }

        private void WriteSourcePrologue(StringWriter output) {
            Context.WriteLine(output, "package mop;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import java.io.*;");
            Context.WriteLine(output, "import java.util.*;");
            Context.WriteLine(output, "import java.text.MessageFormat;");
            Context.WriteLine(output);
            Context.WriteLine(output, "import p.runtime.*;");
            Context.WriteLine(output, "import p.runtime.exceptions.*;");
            Context.WriteLine(output, "import p.runtime.values.*;");

            Context.WriteLine(output, "//add your own imports");
            Context.WriteLine(output);
        }

        private void WriteSpec(StringWriter output, Machine machine)
        {
            Context.WriteLine(output, $"{Context.Names.GetRvmSpecName(machine)} () {{");
            
            BeforeSeparator separator = new BeforeSeparator(() => Context.WriteLine(output));
            
            foreach (PEnum e in EnumDecls.Values)
            {
                separator.beforeElement();
                WriteEnum(output, e);
            }

            foreach (State s in machine.States)
            {
                separator.beforeElement();
                WriteStateClass(output, s);
            }
            Context.WriteLine(output);

            WriteHandlers(output, machine);

            foreach (Function method in machine.Methods)
            {
                // P compiler will extract event handlers as anonymous functions.
                // Since we extract the event handlers already, we can just skip the translation of anonymous functions.
                if (!method.IsAnon)
                {
                    separator.beforeElement();
                    WriteFunction(output, method);
                }
            }
            Context.WriteLine(output);

            WriteMachineFields(output, machine);
            Context.WriteLine(output);

            WriteStateVariable(output, machine);
            Context.WriteLine(output);

            WriteConstructor(output, machine);

            // TODO: Is this the right event set to iterate? Does it contain all events that have anything at all to do with the current machine?
            foreach (PEvent e in machine.Observes.Events)
            {
                Context.WriteLine(output);
                WriteSpecEvent(output, machine, e);
            }

            Context.WriteLine(output, "}");
        }

        private void WriteEnum(StringWriter output, PEnum data)
        {
            string enumName = Context.Names.GetEnumTypeName(data);
            Context.WriteLine(output, $"enum {enumName} {{");

            BeforeSeparator separator = new BeforeSeparator(() => Context.WriteLine(output, ","));
            foreach (EnumElem elem in data.Values)
            {
                string elementName = Context.Names.GetEnumElementName(elem);
                separator.beforeElement();
                Context.Write(output, $"{elementName}({elem.Value})");
            }
            Context.WriteLine(output, ";");

            string enumValue = Context.Names.GetEnumValueName();
            string enumValueGetter = Context.Names.GetEnumValueGetterName();
            Context.WriteLine(output);
            Context.WriteLine(output, $"private int {enumValue};");
            Context.WriteLine(output);
            Context.WriteLine(output, $"private {enumName}(int {enumValue}) {{");
            Context.WriteLine(output, $"this.{enumValue} = {enumValue};");
            Context.WriteLine(output, "}");
            Context.WriteLine(output);
            Context.WriteLine(output, $"public int {enumValueGetter}() {{");
            Context.WriteLine(output, $"return {enumValue};");
            Context.WriteLine(output, "}");
            Context.WriteLine(output, "}");
        }

        private void WriteHandlers(StringWriter output, Machine machine)
        {
            BeforeSeparator separator = new BeforeSeparator(() => Context.WriteLine(output));
            foreach (State state in machine.States)
            {
                foreach (KeyValuePair<PEvent, IStateAction> eventHandler in state.AllEventHandlers)
                {
                    separator.beforeElement();
                    WriteEventHandler(output, state, eventHandler.Key, eventHandler.Value);
                }
            }
            separator.beforeElement();
            WriteStateChangeHandler(output);
            separator.beforeElement();
            WriteRaisedEventHandler(output);
        }

        private void WriteStateClass(StringWriter output, State state)
        {
            string stateClassName = Context.Names.GetStateClassName(state);
            string stateBaseName = Context.Names.GetStateBaseClassName();
            string stateName = Context.Names.GetStateName(state);

            Context.WriteLine(output, $"private class {stateClassName} extends {stateBaseName} {{");
            Context.WriteLine(output, "@Override");
            Context.WriteLine(output, $"public String getName() {{ return \"{stateName}\"; }}");
            if (state.Entry != null)
            {
                Context.WriteLine(output);
                Tools.WriteTemplateEntryHandler(output, (output) => WriteEntryHandlerBody(output, state.Entry));
            }
            if (state.Exit != null)
            {
                Context.WriteLine(output);
                Tools.WriteTemplateExitHandler(output, (output) => WriteFunctionBody(output, state.Exit));
            }
            foreach (KeyValuePair<PEvent, IStateAction> eventHandler in state.AllEventHandlers)
            {
                PEvent pEvent = eventHandler.Key;

                IStateAction action = eventHandler.Value;
    
                Context.WriteLine(output);
                Context.WriteLine(output, "@Override");
                Tools.WriteTemplateEventHandler(output, pEvent, (output) => WriteCallEventHandler(output, state, pEvent, action));
            }
            Context.WriteLine(output, "}");
        }

        private void WriteEntryHandlerBody(StringWriter output, Function handler)
        {
            List<Variable> parameters = handler.Signature.Parameters;
            if (parameters.Count > 0)
            {
                Debug.Assert(parameters.Count == 1);
                Variable argument = parameters[0];
                string argumentType = Context.Names.GetJavaTypeName(argument.Type);
                string argumentName = Context.Names.GetLocalVarName(argument);
                Tools.InlineEventHandlerArguments(output, argumentType, argumentName);
            }
            WriteFunctionBody(output, handler);
        }

        // Extracts each event action in a state as a function.
        private void WriteEventHandler(StringWriter output, State state, PEvent pEvent, IStateAction stateAction)
        {
            string name = Context.Names.GetEventHandlerName(state, pEvent);
            string stateExceptionClass = Context.Names.GetGotoStmtExceptionName();

            switch (stateAction)
            {
                case EventDefer eventDefer:
                    throw new NotImplementedException("Event deferring is not implemented.");

                case EventDoAction eventDoAction: {
                    Function handler = eventDoAction.Target;

                    if (handler.IsAnon)
                    {
                        Context.Write(output, $"private void {name}(");
                        WriteFunctionArguments(output, handler);
                        Context.WriteLine(output, $") {Tools.GetThrowsClause()} {{");
                        WriteFunctionBody(output, handler);
                        Context.WriteLine(output, "}");
                    }
                    break;
                }

                case EventGotoState eventGotoState when eventGotoState.TransitionFunction == null:
                    break;

                case EventGotoState eventGotoState when eventGotoState.TransitionFunction != null:
                {
                    var transitionFunc = eventGotoState.TransitionFunction;

                    if (transitionFunc.IsAnon)
                    {
                        Context.Write(output, $"private void {name}(");
                        WriteFunctionArguments(output, transitionFunc);
                        Context.WriteLine(output, $") {Tools.GetThrowsClause()} {{");

                        WriteFunctionBody(output, transitionFunc);

                        Context.WriteLine(output, "}");
                    }
                    break;
                }
                case EventIgnore _:
                    throw new NotImplementedException("Event ignoring is not implemented.");

                case EventPushState eventPushState:
                    throw new NotImplementedException("State pushing is not implemented.");

                default:
                    throw new ArgumentOutOfRangeException(stateAction.GetType().FullName);
            }
        }

        private void WriteMachineFields(StringWriter output, Machine machine)
        {
            foreach (Variable field in machine.Fields)
            {
                string variableType = Context.Names.GetJavaTypeName(field.Type);
                string variableName = Context.Names.GetNameForDecl(field);
                string defaultValue = GetBoxedDefaultValue(field.Type);
                Context.WriteLine(output,
                    $"private {variableType} {variableName} = {defaultValue};");
            }
        }

        private void WriteStateVariable(StringWriter output, Machine machine)
        {
            string baseClass = Context.Names.GetStateBaseClassName();
            string stateVariable = Context.Names.GetStateVariableName();
            string initState = Context.Names.GetStateClassName(machine.StartState);
            Context.WriteLine(output, $"{baseClass} {stateVariable} = new {initState}();");
        }

        private void WriteConstructor(StringWriter output, Machine machine)
        {
            string constructorName = Context.Names.GetSpecConstructorName(machine);
            string stateVariable = Context.Names.GetStateVariableName();
            string entryHandler = Context.Names.GetEntryHandlerName();
            // TODO: Also consider the case when the constructor has arguments.
            Context.WriteLine(output, $"public {constructorName}() {{");
            WrapInTryStmtException(
                output,
                (output) => Context.WriteLine(output, $"{stateVariable}.{entryHandler}(Optional.empty());"));
            Context.WriteLine(output, "}");
        }

        private void WriteSpecEvent(StringWriter output, Machine machine, PEvent pEvent)
        {
            string eventName = Context.Names.GetRvmEventName(pEvent);
            string stateVariable = Context.Names.GetStateVariableName();
            Context.Write(output, $"event {eventName} (");
            string payloadName;
            if (!Tools.isNullType(pEvent.PayloadType)) {
                string payloadType = Context.Names.GetJavaTypeName(pEvent.PayloadType);
                payloadName = Context.Names.GetPayloadArgumentName();
                Context.Write(output, $"{payloadType} {payloadName}");
            }
            else
            {
                payloadName = "";
            }
            Context.WriteLine(output, ") {");

            string handlerName = Context.Names.GetStateEventHandlerName(pEvent);
            WrapInTryStmtException(
                output,
                (output) => Context.WriteLine(output, $"{stateVariable}.{handlerName}({payloadName});"));

            Context.WriteLine(output, "}");
        }

        private void WriteCallEventHandler(StringWriter output, State currentState, PEvent pEvent, IStateAction stateAction)
        {
            string name = Context.Names.GetEventHandlerName(currentState, pEvent);

            string payloadName = "Optional.empty()";
            if (!Tools.isNullType(pEvent.PayloadType)) {
                payloadName = $"Optional.of({Context.Names.GetPayloadArgumentName()})";
            }

            switch (stateAction)
            {
                case EventDefer eventDefer:
                    throw new NotImplementedException("Event deferring is not implemented.");

                case EventDoAction eventDoAction:
                    WriteCallEventFunction(output, name, pEvent, eventDoAction.Target);
                    break;

                case EventGotoState eventGotoState when eventGotoState.TransitionFunction == null:
                {
                    string stateClass = Context.Names.GetStateClassName(eventGotoState.Target);
                    string exceptionClass = Context.Names.GetGotoStmtExceptionName();
                    Context.WriteLine(output, $"throw new {exceptionClass}(new {stateClass}(), {payloadName});");
                    break;
                }

                case EventGotoState eventGotoState when eventGotoState.TransitionFunction != null:
                {
                    var transitionFunc = eventGotoState.TransitionFunction;

                    WriteCallEventFunction(output, name, pEvent, transitionFunc);
                    
                    string stateClass = Context.Names.GetStateClassName(eventGotoState.Target);
                    string exceptionClass = Context.Names.GetGotoStmtExceptionName();
                    Context.WriteLine(output, $"throw new {exceptionClass}(new {stateClass}(), {payloadName});");

                    break;
                }
                case EventIgnore _:
                    throw new NotImplementedException("Event ignoring is not implemented.");

                case EventPushState eventPushState:
                    throw new NotImplementedException("State pushing is not implemented.");

                default:
                    throw new ArgumentOutOfRangeException(stateAction.GetType().FullName);
            }
        }

        private void WriteCallEventFunction(StringWriter output, string name, PEvent pEvent, Function function)
        {
            List<Variable> parameters = function.Signature.Parameters;
            Debug.Assert(parameters.Count <= 1);

            if (function.IsAnon) 
            {
                Context.Write(output, $"{name}(");
                if (parameters.Count == 1)
                {
                    // TODO: Subtypes should also be fine. Is IsAssignableFrom the proper method to use?
                    Debug.Assert(parameters[0].Type.IsSameTypeAs(pEvent.PayloadType));
                    string eventArg = Context.Names.GetPayloadArgumentName();
                    Context.Write(output, eventArg);
                }
                Context.WriteLine(output, ");");
            }
            else
            {
                List<string> args = new List<string>();
                if (parameters.Count == 1)
                {
                    args.Add(Context.Names.GetPayloadArgumentName());
                }
                WriteStringFunctionCall(output, function.Name, args);
                Context.WriteLine(output, ";");
            }
        }

        private void WriteFunction(StringWriter output, Function function)
        {
            if (function.IsForeign)
            {
                throw new NotImplementedException("Foreign function is not implemented.");
            }

            bool isStatic = function.Owner == null;

            if (isStatic) {
                throw new NotImplementedException("Static function is not implemented.");
            }

            FunctionSignature signature = function.Signature;
            string returnType = Context.Names.GetJavaTypeName(signature.ReturnType);
            string functionName = Context.Names.GetNameForDecl(function);
            string throwsClause = Tools.GetThrowsClause();
            Context.Write(output, $"public {returnType} {functionName}(");
            WriteFunctionArguments(output, function);
            Context.WriteLine(output, $") {throwsClause} {{");
            WriteFunctionBody(output, function);
            Context.WriteLine(output, "}");
        }

        private void WriteFunctionBody(StringWriter output, Function function)
        {
            foreach (Variable local in function.LocalVariables)
            {
                string variableType = Context.Names.GetJavaTypeName(local.Type);
                string variableName = Context.Names.GetNameForDecl(local);
                string defaultValue = GetBoxedDefaultValue(local.Type);
                Context.WriteLine(output,
                    $"{variableType} {variableName} = {defaultValue};");
            }

            foreach (IPStmt bodyStatement in function.Body.Statements)
            {
                WriteStmt(output: output, function: function, stmt: bodyStatement);
            }
        }

        private void WriteStmt(StringWriter output, Function function, IPStmt stmt)
        {
            switch (stmt)
            {
                case AnnounceStmt announceStmt:
                    throw new NotImplementedException("AnnounceStmt is not implemented.");

                case AssertStmt assertStmt:
                    Context.Write(output, "Assertion.rvmAssert(");
                    UnboxIfNeeded(output, (output) => WriteExpr(output, assertStmt.Assertion));
                    Context.Write(output, ", ");
                    Context.Write(output, "(\"Assertion Failed: \" + ");
                    WriteExpr(output, assertStmt.Message);
                    Context.WriteLine(output, "));");
                    break;

                case AssignStmt assignStmt:
                    WriteAssignStmt(output, assignStmt.Location, (output) => WriteExpr(output, assignStmt.Value));
                    break;

                case CompoundStmt compoundStmt:
                    Context.WriteLine(output, "{");
                    foreach (IPStmt subStmt in compoundStmt.Statements)
                    {
                        WriteStmt(output, function, subStmt);
                    }

                    Context.WriteLine(output, "}");
                    break;

                case CtorStmt ctorStmt:
                    throw new NotImplementedException("CtorStmt is not implemented.");

                case FunCallStmt funCallStmt:
                    WriteFunctionCall(output, funCallStmt.Function, funCallStmt.ArgsList);
                    Context.WriteLine(output, ";");
                    break;

                case GotoStmt gotoStmt:
                    //last statement
                    string stateClass = Context.Names.GetStateClassName(gotoStmt.State);
                    string gotoExceptionClass = Context.Names.GetGotoStmtExceptionName();
                    Context.Write(output, $"throw new {gotoExceptionClass}(new {stateClass}(), ");

                    if (gotoStmt.Payload != null)
                    {
                        Context.Write(output, "Optional.of(");
                        WriteBoxedExpression(output, gotoStmt.Payload);
                        Context.Write(output, ")");
                    }
                    else
                    {
                        Context.Write(output, "Optional.empty()");
                    }

                    Context.WriteLine(output, ");");
                    break;

                case IfStmt ifStmt:
                    Context.Write(output, "if (");
                    UnboxIfNeeded(output, (output) => WriteExpr(output, ifStmt.Condition));
                    Context.WriteLine(output, ")");
                    WriteStmt(output, function, ifStmt.ThenBranch);
                    if (ifStmt.ElseBranch != null && ifStmt.ElseBranch.Statements.Any())
                    {
                        Context.WriteLine(output, "else");
                        WriteStmt(output, function, ifStmt.ElseBranch);
                    }
                    break;

                case AddStmt addStmt:
                {
                    WriteExpr(output, addStmt.Variable);
                    string insertFuncName = Context.Names.GetInsertFunc();
                    Context.Write(output, $".{insertFuncName}(");
                    WriteBoxedExpression(output, addStmt.Value);
                    Context.WriteLine(output, ");");
                    break;
                }

                case InsertStmt insertStmt:
                    switch (insertStmt.Variable.Type.Canonicalize())
                    {
                        case MapType _:
                        {
                            WriteExpr(output, insertStmt.Variable);
                            string insertFuncName = Context.Names.GetInsertFunc();
                            Context.Write(output, $".{insertFuncName}(");
                            WriteBoxedExpression(output, insertStmt.Index);
                            Context.Write(output, ", ");
                            WriteBoxedExpression(output, insertStmt.Value);
                            Context.WriteLine(output, ");");
                            break;
                        }

                        case SequenceType _:
                        {
                            WriteExpr(output, insertStmt.Variable);
                            string insertFuncName = Context.Names.GetInsertFunc();
                            Context.Write(output, $".{insertFuncName}((int)");
                            UnboxIfNeeded(
                                output,
                                (output) => WriteExpr(output, insertStmt.Index));
                            Context.Write(output, ", ");
                            WriteBoxedExpression(output, insertStmt.Value);
                            Context.WriteLine(output, ");");
                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException(
                                $"Insert stmt cannot be applied to type {insertStmt.Variable.Type.OriginalRepresentation}");
                    }
                    break;

                case MoveAssignStmt moveAssignStmt:
                    if (!moveAssignStmt.FromVariable.Type.IsSameTypeAs(moveAssignStmt.ToLocation.Type))
                    {
                        throw new NotImplementedException("Typecasting in MoveAssignStmt is not implemented.");
                    }
                    string fromVariableName = Context.Names.GetNameForDecl(moveAssignStmt.FromVariable);
                    WriteAssignStmt(
                        output,
                        moveAssignStmt.ToLocation,
                        (output) => {Context.Write(output, $"{fromVariableName}"); return BoxingState.BOXED;});
                    break;

                case NoStmt _:
                    break;

                case PopStmt _:
                    //last statement
                    throw new NotImplementedException("PopStmt is not implemented.");

                case PrintStmt printStmt:
                    Context.Write(output, "System.out.println(");
                    WriteExpr(output, printStmt.Message);
                    Context.WriteLine(output, ");");
                    break;

                case RaiseStmt raiseStmt:
                    //last statement
                    string raiseExceptionClass = Context.Names.GetRaiseStmtExceptionName();
                    Context.Write(output, $"throw new {raiseExceptionClass}(");
                    WriteExpr(output, raiseStmt.PEvent);
                    Context.Write(output, ", ");
                    if (raiseStmt.Payload.Any())
                    {
                        Context.Write(output, "Optional.of(");
                        WriteBoxedExpression(output, raiseStmt.Payload.First());
                        Context.Write(output, ")");
                    } else {
                        Context.Write(output, "Optional.empty()");
                    }
                    Context.WriteLine(output, ");");
                    break;

                case ReceiveStmt receiveStmt:
                    throw new NotImplementedException("ReceiveStmt is not implemented.");

                case RemoveStmt removeStmt:
                    switch (removeStmt.Variable.Type.Canonicalize())
                    {
                        case MapType _:
                        case SetType _:
                        {
                            WriteExpr(output, removeStmt.Variable);
                            string removeFuncName = Context.Names.GetRemoveFunc();
                            Context.Write(output, $".{removeFuncName}(");
                            WriteBoxedExpression(output, removeStmt.Value);
                            Context.WriteLine(output, ");");
                            break;
                        }

                        case SequenceType _:
                        {
                            WriteExpr(output, removeStmt.Variable);
                            string removeFuncName = Context.Names.GetRemoveFunc();
                            Context.Write(output, $".{removeFuncName}((int)");
                            UnboxIfNeeded(output, (output) => WriteExpr(output, removeStmt.Value));
                            Context.WriteLine(output, ");");
                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException(
                                $"Remove cannot be applied to type {removeStmt.Variable.Type.OriginalRepresentation}");
                    }
                    break;

                case ReturnStmt returnStmt:
                    Context.Write(output, "return");
                    if (returnStmt.ReturnValue != null)
                    {
                        Context.Write(output, " ");
                        BoxIfNeeded(output, returnStmt.ReturnType, (output) => WriteExpr(output, returnStmt.ReturnValue));
                    }
                    Context.WriteLine(output, ";");
                    break;

                case BreakStmt breakStmt:
                    Context.WriteLine(output, "break;");
                    break;

                case ContinueStmt continueStmt:
                    Context.WriteLine(output, "continue;");
                    break;

                case SendStmt sendStmt:
                    throw new NotImplementedException("SendStmt is not implemented.");

                case SwapAssignStmt swapStmt:
                    throw new NotImplementedException("SwapAssignStmt is not implemented.");

                case WhileStmt whileStmt:
                    Context.Write(output, "while (");
                    UnboxIfNeeded(output, (output) => WriteExpr(output, whileStmt.Condition));
                    Context.WriteLine(output, ")");
                    WriteStmt(output, function, whileStmt.Body);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(stmt));
            }
        }

        private delegate BoxingState AssignStmtRightValueDelegate(StringWriter output);

        private void WriteAssignStmt(StringWriter output, IPExpr lvalue, AssignStmtRightValueDelegate writeRightValue)
        {
            switch (lvalue) {
                case MapAccessExpr mapAccessExpr:
                {
                    WriteExpr(output, mapAccessExpr.MapExpr);
                    string putFuncName = Context.Names.GetMapPutFunc();
                    Context.Write(output, $".{putFuncName}(");
                    WriteBoxedExpression(output, mapAccessExpr.IndexExpr);
                    Context.Write(output, ", ");
                    BoxIfNeeded(output, lvalue.Type, (output) => writeRightValue(output));
                    Context.WriteLine(output, ");");
                    break;
                }

                case NamedTupleAccessExpr namedTupleAccessExpr:
                {
                    WriteExpr(output, namedTupleAccessExpr.SubExpr);
                    string setterName = Context.Names.GetTupleFieldSetter();
                    string fieldName = namedTupleAccessExpr.FieldName;
                    Context.Write(output, $".{setterName}(\"{fieldName}\", ");
                    BoxIfNeeded(output, lvalue.Type, (output) => writeRightValue(output));
                    Context.WriteLine(output, ");");
                    break;
                }

                case SeqAccessExpr seqAccessExpr:
                {
                    WriteExpr(output, seqAccessExpr.SeqExpr);
                    string setFuncName = Context.Names.GetSeqSetIndexFunc();
                    Context.Write(output, $".{setFuncName}((int)");
                    UnboxIfNeeded(output, (output) => WriteExpr(output, seqAccessExpr.IndexExpr));
                    Context.Write(output, ", ");
                    BoxIfNeeded(output, lvalue.Type, (output) => writeRightValue(output));
                    Context.WriteLine(output, ");");
                    break;
                }

                case TupleAccessExpr tupleAccessExpr:
                {
                    WriteExpr(output, tupleAccessExpr.SubExpr);
                    string setterName = Context.Names.GetTupleFieldSetter();
                    int fieldNo = tupleAccessExpr.FieldNo;
                    Context.Write(output, $".{setterName}({fieldNo}, ");
                    BoxIfNeeded(output, lvalue.Type, (output) => writeRightValue(output));
                    Context.WriteLine(output, ");");
                    break;
                }

                case VariableAccessExpr variableAccessExpr:
                    Context.Write(output, Context.Names.GetNameForDecl(variableAccessExpr.Variable));
                    Context.Write(output, " = ");
                    BoxIfNeeded(output, lvalue.Type, (output) => writeRightValue(output));
                    Context.WriteLine(output, ";");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(lvalue));
            }
        }

        private BoxingState WriteRValue(StringWriter output, IPExpr rvalue)
        {
            switch (rvalue)
            {
                case MapAccessExpr mapAccessExpr:
                {
                    string resultType = Context.Names.GetJavaTypeName(mapAccessExpr.Type);
                    Context.Write(output, $"(({resultType})");
                    WriteExpr(output, mapAccessExpr.MapExpr);
                    string getFunc = Context.Names.GetGetFunc();
                    Context.Write(output, $".{getFunc}(");
                    WriteBoxedExpression(output, mapAccessExpr.IndexExpr);
                    Context.Write(output, "))"); 
                    return BoxedOrNotApplicable(mapAccessExpr.Type);
                 }

                case NamedTupleAccessExpr namedTupleAccessExpr:
                {
                    string resultType = Context.Names.GetJavaTypeName(namedTupleAccessExpr.Type);
                    Context.Write(output, $"(({resultType})");
                    WriteExpr(output, namedTupleAccessExpr.SubExpr);
                    string getterName = Context.Names.GetTupleFieldGetter();
                    string fieldName = namedTupleAccessExpr.FieldName;
                    Context.Write(output, $".{getterName}(\"{fieldName}\")");
                    Context.Write(output, ")");
                    return BoxedOrNotApplicable(namedTupleAccessExpr.Type);
                 }

                case SeqAccessExpr seqAccessExpr:
                {
                    string resultType = Context.Names.GetJavaTypeName(seqAccessExpr.Type);
                    Context.Write(output, $"(({resultType})");
                    WriteExpr(output, seqAccessExpr.SeqExpr);
                    string getFunc = Context.Names.GetGetFunc();
                    Context.Write(output, $".{getFunc}((int)");
                    UnboxIfNeeded(output, (output) => WriteExpr(output, seqAccessExpr.IndexExpr));
                    Context.Write(output, "))");
                    return BoxedOrNotApplicable(seqAccessExpr.Type);
                }

                case TupleAccessExpr tupleAccessExpr:
                {
                    string resultType = Context.Names.GetJavaTypeName(tupleAccessExpr.Type);
                    Context.Write(output, $"(({resultType})");
                    WriteExpr(output, tupleAccessExpr.SubExpr);
                    string getterName = Context.Names.GetTupleFieldGetter();
                    int fieldNo = tupleAccessExpr.FieldNo;
                    Context.Write(output, $".{getterName}({fieldNo})");
                    Context.Write(output, ")");
                    return BoxedOrNotApplicable(tupleAccessExpr.Type);
                }

                case VariableAccessExpr variableAccessExpr:
                {
                    Context.Write(output, Context.Names.GetNameForDecl(variableAccessExpr.Variable));
                    return BoxedOrNotApplicable(variableAccessExpr.Type);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(rvalue));
            }
        }

        private BoxingState WriteExpr(StringWriter output, IPExpr pExpr)
        {
            switch (pExpr)
            {
                case CloneExpr cloneExpr:
                    return WriteClone(output, cloneExpr.Term);

                case BinOpExpr binOpExpr:
                    WriteBinaryOp(output, binOpExpr.Lhs, binOpExpr.Operation, binOpExpr.Rhs);
                    if (!isBoxable(pExpr.Type))
                    {
                        throw new InvalidStateException($"Unexpected binary operation for unboxable type: {pExpr.Type.ToString()}.");
                    }
                    return BoxingState.UNBOXED;

                case BoolLiteralExpr boolLiteralExpr:
                    Context.Write(output, $"({(boolLiteralExpr.Value ? "true" : "false")})");
                    return BoxingState.UNBOXED;

                case CastExpr castExpr:
                    string castTypeName = Context.Names.GetJavaTypeName(castExpr.Type); 
                    Context.Write(output, $"(({castTypeName})");
                    WriteBoxedExpression(output, castExpr.SubExpr);
                    Context.Write(output, ")");
                    return BoxingState.BOXED;

                case CoerceExpr coerceExpr:
                    return WriteCoerceExpr(output, coerceExpr);

                case ChooseExpr chooseExpr:
                    throw new NotImplementedException("ChooseExpr is not implemented.");

                case ContainsExpr containsExpr:
                    var isMap = PLanguageType.TypeIsOfKind(containsExpr.Collection.Type, TypeKind.Map);
                    Context.Write(output, $"(new BoolValue(");
                    WriteExpr(output, containsExpr.Collection);
                    string containsFunc = Context.Names.GetContainsFunc(isMap);
                    Context.Write(output, $".{containsFunc}(");
                    WriteBoxedExpression(output, containsExpr.Item);
                    Context.Write(output, ")))");
                    return BoxingState.BOXED;

                case CtorExpr ctorExpr:
                    throw new NotImplementedException("CtorExpr is not implemented.");

                case DefaultExpr defaultExpr:
                    Context.Write(output, GetUnboxedDefaultValue(defaultExpr.Type));
                    if (isBoxable(defaultExpr.Type))
                    {
                        return BoxingState.UNBOXED;
                    }
                    return BoxingState.NOT_APPLICABLE;

                case EnumElemRefExpr enumElemRefExpr:
                    string typeName = Context.Names.GetEnumTypeName(enumElemRefExpr.Value.ParentEnum);
                    string valueName = Context.Names.GetEnumElementName(enumElemRefExpr.Value);
                    Context.Write(output, $"{typeName}.{valueName}");
                    return BoxingState.UNBOXED;

                case EventRefExpr eventRefExpr:
                    string eventClassName = Context.Names.GetQualifiedEventClassName(eventRefExpr.Value);
                    switch (eventClassName)
                    {
                        case "Halt":
                            throw new NotImplementedException("Halt is not implemented.");

                        case "DefaultEvent":
                            throw new NotImplementedException("DefaultEvent is not implemented.");

                        default:
                            Context.Write(output, $"new {eventClassName}()");
                            break;
                    }
                    return BoxingState.NOT_APPLICABLE;

                case FairNondetExpr _:
                    throw new NotImplementedException("FairNondetExpr is not implemented.");

                case FloatLiteralExpr floatLiteralExpr:
                    Context.Write(output, $"({floatLiteralExpr.Value}d)");
                    return BoxingState.UNBOXED;

                case FunCallExpr funCallExpr:
                    WriteFunctionCall(output, funCallExpr.Function, funCallExpr.Arguments);
                    return BoxedOrNotApplicable(funCallExpr.Type);

                case IntLiteralExpr intLiteralExpr:
                    Context.Write(output, $"({intLiteralExpr.Value}L)");
                    return BoxingState.UNBOXED;

                case KeysExpr keysExpr:
                    Context.Write(output, "(");
                    WriteExpr(output, keysExpr.Expr);
                    string cloneKeysFunc = Context.Names.GetMapCloneKeysFunc();
                    Context.Write(output, $").{cloneKeysFunc}()");
                    return BoxingState.NOT_APPLICABLE;

                case LinearAccessRefExpr linearAccessRefExpr:
                    Context.Write(output, $"{Context.Names.GetNameForDecl(linearAccessRefExpr.Variable)}");
                    return BoxingState.BOXED;

                case NamedTupleExpr namedTupleExpr:
                {
                    string namedTupleClass = Context.Names.GetNamedTupleTypeName();
                    string valueInterface = Context.Names.GetValueInterfaceName();

                    NamedTupleType ntType = (NamedTupleType)namedTupleExpr.Type;

                    Context.Write(output, $"new {namedTupleClass}(new String[]{{");
                    BeforeSeparator namesSeparator = new BeforeSeparator(() => Context.Write(output, ", "));
                    for (int i = 0; i < ntType.Fields.Count; i++)
                    {
                        namesSeparator.beforeElement();
                        Context.Write(output, $"\"{Context.Names.GetTupleFieldName(ntType.Fields[i].Name)}\"");
                    }

                    Context.Write(output, $"}}, new {valueInterface}<?>[]{{");
                    BeforeSeparator valuesSeparator = new BeforeSeparator(() => Context.Write(output, ", "));
                    for (int i = 0; i < ntType.Fields.Count; i++)
                    {
                        valuesSeparator.beforeElement();
                        WriteBoxedExpression(output, namedTupleExpr.TupleFields[i]);
                    }

                    Context.Write(output, "})");
                    return BoxingState.NOT_APPLICABLE;
                }

                case NondetExpr _:
                    throw new NotImplementedException("NondetExpr is not implemented.");

                case NullLiteralExpr _:
                    Context.Write(output, "null");
                    return BoxingState.NOT_APPLICABLE;

                case SizeofExpr sizeofExpr:
                    WriteExpr(output, sizeofExpr.Expr);
                    string sizeFuncName = Context.Names.GetSizeFunc();
                    Context.Write(output, $".{sizeFuncName}()");
                    return BoxingState.UNBOXED;

                case StringExpr stringExpr:
                    if (stringExpr.Args.Count == 0)
                    {
                        Context.Write(output, $"\"{stringExpr.BaseString}\"");
                    }
                    else
                    {
                        string baseString = TransformPrintMessage(stringExpr.BaseString);
                        Context.Write(output, $"(MessageFormat.format(");
                        Context.Write(output,  $"\"{baseString}\"");
                        foreach (var arg in stringExpr.Args)
                        {
                            Context.Write(output, ", ");
                            WriteExpr(output, arg);
                        }
                        Context.Write(output, "))");
                    }
                    return BoxingState.UNBOXED;

                case ThisRefExpr _:
                    throw new NotImplementedException("ThisRefExpr is not implemented.");

                case UnaryOpExpr unaryOpExpr:
                    Context.Write(output, $"{UnOpToStr(unaryOpExpr.Operation)}(");
                    UnboxIfNeeded(output, (output) => WriteExpr(output, unaryOpExpr.SubExpr));
                    Context.Write(output, ")");
                    if (!isBoxable(pExpr.Type))
                    {
                        throw new InvalidStateException($"Unexpected unary operation for unboxable type: {pExpr.Type.ToString()}.");
                    }
                    return BoxingState.UNBOXED;

                case UnnamedTupleExpr unnamedTupleExpr:
                {
                    string tupleClass = Context.Names.GetTupleTypeName();
                    string valueInterface = Context.Names.GetValueInterfaceName();

                    Context.Write(output, $"new {tupleClass}(new {valueInterface}<?>[]{{");
                    BeforeSeparator valuesSeparator = new BeforeSeparator(() => Context.Write(output, ", "));
                    foreach (IPExpr field in unnamedTupleExpr.TupleFields)
                    {
                        valuesSeparator.beforeElement();
                        WriteBoxedExpression(output, field);
                    }
                    Context.Write(output, "})");
                    return BoxingState.NOT_APPLICABLE;
                }

                case ValuesExpr valuesExpr:
                    Context.Write(output, "(");
                    WriteExpr(output, valuesExpr.Expr);
                    string cloneValuesFunc = Context.Names.GetMapCloneValuesFunc();
                    Context.Write(output, $").{cloneValuesFunc}()");
                    return BoxingState.NOT_APPLICABLE;

                case MapAccessExpr _:
                case NamedTupleAccessExpr _:
                case SeqAccessExpr _:
                case TupleAccessExpr _:
                case VariableAccessExpr _:
                    return WriteRValue(output, pExpr);

                default:
                    throw new ArgumentOutOfRangeException(nameof(pExpr), $"type was {pExpr?.GetType().FullName}");
            }
        }

        private BoxingState WriteCoerceExpr(StringWriter output, CoerceExpr coerceExpr)
        {
            switch (coerceExpr.NewType.Canonicalize())
            {
                case PrimitiveType newType when newType.IsSameTypeAs(PrimitiveType.Float):
                    Context.Write(output, "(float)");
                    WriteCoerceSubExpr(output, coerceExpr.SubExpr);
                    return BoxingState.UNBOXED;
                
                case PrimitiveType newType1 when newType1.IsSameTypeAs(PrimitiveType.Int):
                    Context.Write(output, "(int)");
                    WriteCoerceSubExpr(output, coerceExpr.SubExpr);
                    return BoxingState.UNBOXED;

                default:
                    throw new ArgumentOutOfRangeException(
                        @"unexpected coercion operation to:" + coerceExpr.Type.CanonicalRepresentation);
            }
        }

        private void WriteCoerceSubExpr(StringWriter output, IPExpr expr)
        {
            switch (expr.Type.Canonicalize())
            {
                case PrimitiveType newType when newType.IsSameTypeAs(PrimitiveType.Float):
                case PrimitiveType newType1 when newType1.IsSameTypeAs(PrimitiveType.Int):
                    Context.Write(output, "(");
                    UnboxIfNeeded(output, (output) => WriteExpr(output, expr));
                    Context.Write(output, ")");
                    break;

                case EnumType enumType:
                    string valueGetter = Context.Names.GetEnumValueGetterName();
                    Context.Write(output, "(");
                    UnboxIfNeeded(output, (output) => WriteExpr(output, expr));
                    Context.Write(output, $").{valueGetter}()");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        @"unexpected coercion operation from:" + expr.Type.CanonicalRepresentation);
            }
        }

        private void WriteBoxedExpression(StringWriter output, IPExpr expr)
        {
            BoxIfNeeded(output, expr.Type, output => WriteExpr(output, expr));
        }

        private void WriteBinaryOp(StringWriter output, IPExpr left, BinOpType binOpType, IPExpr right)
        {
            switch (binOpType)
            {
                case BinOpType.Add:
                    WriteSimpleBinaryOp(output, left, "+", right);
                    break;

                case BinOpType.Sub:
                    WriteSimpleBinaryOp(output, left, "-", right);
                    break;

                case BinOpType.Mul:
                    WriteSimpleBinaryOp(output, left, "*", right);
                    break;

                case BinOpType.Div:
                    WriteSimpleBinaryOp(output, left, "/", right);
                    break;

                case BinOpType.Lt:
                    WriteSimpleBinaryOp(output, left, "<", right);
                    break;

                case BinOpType.Le:
                    WriteSimpleBinaryOp(output, left, "<=", right);
                    break;

                case BinOpType.Gt:
                    WriteSimpleBinaryOp(output, left, ">", right);
                    break;

                case BinOpType.Ge:
                    WriteSimpleBinaryOp(output, left, ">=", right);
                    break;

                case BinOpType.And:
                    WriteSimpleBinaryOp(output, left, "&&", right);
                    break;

                case BinOpType.Or:
                    WriteSimpleBinaryOp(output, left, "||", right);
                    break;

                case BinOpType.Eq: {
                    string iValueClass = Context.Names.GetValueInterfaceName();
                    string safeEquals = Context.Names.GetValueInterfaceSafeEqualsName();
                    Context.Write(output, $"{iValueClass}.{safeEquals}(");
                    // TODO: If both subexpressions are unboxed, then we should
                    // use '==' directly.
                    WriteBoxedExpression(output, left);
                    Context.Write(output, ", ");
                    WriteBoxedExpression(output, right);
                    Context.Write(output, ")");
                    break;
                }

                case BinOpType.Neq: {
                    string iValueClass = Context.Names.GetValueInterfaceName();
                    string safeEquals = Context.Names.GetValueInterfaceSafeEqualsName();
                    Context.Write(output, $"(!{iValueClass}.{safeEquals}(");
                    // TODO: If both subexpressions are unboxed, then we should
                    // use '==' directly.
                    WriteBoxedExpression(output, left);
                    Context.Write(output, ", ");
                    WriteBoxedExpression(output, right);
                    Context.Write(output, "))");
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(binOpType), binOpType, null);
            }
        }

        private void WriteSimpleBinaryOp(StringWriter output, IPExpr left, string binOp, IPExpr right) {
            Context.Write(output, "(");
            // TODO: handle enum
            UnboxIfNeeded(output, (output) => WriteExpr(output, left));
            Context.Write(output, $") {binOp} (");
            UnboxIfNeeded(output, (output) => WriteExpr(output, right));
            Context.Write(output, ")");
        }


        private BoxingState WriteClone(StringWriter output, IExprTerm cloneExprTerm)
        {
            if (cloneExprTerm is IVariableRef variableRef)
            {
                // TODO: This entire if branch should probably be replaced by one
                // writeSimpleClone call.
                PLanguageType type = variableRef.Type;
                switch (type.Canonicalize())
                {
                    case DataType _:
                        throw new NotImplementedException("Cloning DataType is not implemented.");

                    case EnumType _:
                        return writeSimpleClone(output, cloneExprTerm);

                    case ForeignType _:
                        throw new NotImplementedException("Cloning ForeignType is not implemented.");

                    case MapType _:
                    case NamedTupleType _:
                    case TupleType _:
                    case SequenceType _:
                    case SetType _:
                        return writeSimpleClone(output, cloneExprTerm);

                    case PermissionType _:
                        throw new NotImplementedException("Cloning PermissionType is not implemented.");

                    case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                        return writeSimpleClone(output, cloneExprTerm);

                    case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                        return writeSimpleClone(output, cloneExprTerm);

                    case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                        return writeSimpleClone(output, cloneExprTerm);

                    case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                        return writeSimpleClone(output, cloneExprTerm);

                    case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                        throw new NotImplementedException("Cloning AnyType is not implemented.");

                    case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                        throw new NotImplementedException("Cloning EventType is not implemented.");

                    case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                        throw new NotImplementedException("Cloning MachineType is not implemented.");

                    case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                        throw new NotImplementedException("Cloning Null is not implemented.");

                    default:
                        throw new ArgumentOutOfRangeException(type.ToString());
                }
            }
            else
            {
                return WriteExpr(output, cloneExprTerm);
            }
        }

        private BoxingState writeSimpleClone(StringWriter output, IExprTerm expr)
        {
            StringWriter exprBuffer = new StringWriter();
            BoxingState state = WriteExpr(exprBuffer, expr);
            string exprString = exprBuffer.ToString();
            switch (state)
            {
                case BoxingState.UNBOXED:
                    Context.Write(output, exprString);
                    return BoxingState.UNBOXED;
                case BoxingState.BOXED:
                case BoxingState.NOT_APPLICABLE:
                    string iValueClass = Context.Names.GetValueInterfaceName();
                    string safeClone = Context.Names.GetSafeCloneFunctionName();
                    Context.Write(output, $"{iValueClass}.{safeClone}(");
                    Context.Write(output, exprString);
                    Context.Write(output, ")");
                    return state;
                default:
                    throw new ArgumentOutOfRangeException(state.ToString());
            }
        }

        private void WriteFunctionCall(StringWriter output, Function function, IEnumerable<IPExpr> arguments)
        {
            bool isStatic = function.Owner == null;
            if (isStatic)
            {
                throw new NotImplementedException("StaticFunCallExpr is not implemented.");
            }

            Context.Write(output, $"{Context.Names.GetNameForDecl(function)}(");

            BeforeSeparator separator = new BeforeSeparator(() => Context.Write(output, ", "));
            foreach (IPExpr param in arguments)
            {
                separator.beforeElement();
                WriteBoxedExpression(output, param);
            }

            Context.Write(output, ")");
        }

        private void WriteStringFunctionCall(StringWriter output, string functionName, List<string> arguments)
        {
            Context.Write(output, $"{functionName}(");
            BeforeSeparator separator = new BeforeSeparator(() => Context.Write(output, ", "));
            foreach (string argument in arguments)
            {
                separator.beforeElement();
                Context.Write(output, argument);
            }
            Context.Write(output, ")");
        }

        private void WriteCallChangeStateHandler(StringWriter output, string stateValue, string payload)
        {
            WriteStringFunctionCall(
                output,
                Context.Names.GetChangeStateFunctionName(),
                new List<string>(){stateValue, payload});
            Context.WriteLine(output, ";");
        }

        private void WriteCallStateEntryHandler(StringWriter output)
        {
            string stateVariable = Context.Names.GetStateVariableName();
            string handlerName = Context.Names.GetEntryHandlerName();

            List<string> arguments = new List<string>() { Context.Names.GetPayloadArgumentName() };
            WriteStringFunctionCall(output, $"{stateVariable}.{handlerName}", arguments);
            Context.WriteLine(output, ";");
        }

        private void WriteCallStateExitHandler(StringWriter output)
        {
            string stateVariable = Context.Names.GetStateVariableName();
            string handlerName = Context.Names.GetExitHandlerName();

            WriteStringFunctionCall(output, $"{stateVariable}.{handlerName}", new List<string>() { });
            Context.WriteLine(output, ";");
        }

        private void WriteCallRaisedEventHandler(StringWriter output, string eventArg, string payload)
        {
            WriteStringFunctionCall(
                output,
                Context.Names.GetHandleRaisedEventFunctionName(),
                new List<string>(){eventArg, payload});
            Context.WriteLine(output, ";");
        }

        // Writes changeStateTo function which handles goto functionality.
        private void WriteStateChangeHandler(StringWriter output)
        {
            string stateClass = Context.Names.GetStateBaseClassName();
            string changeStateFunction = Context.Names.GetChangeStateFunctionName();
            string stateName = Context.Names.GetNextStateArgumentName();
            string payloadType = Context.Names.GetDefaultPayloadTypeName();
            string payloadName = Context.Names.GetPayloadArgumentName();
            Context.WriteLine(
                output,
                $"private void {changeStateFunction}({stateClass} {stateName}, Optional<{payloadType}> {payloadName}) {{");
            WrapInTryStmtException(
                output,
                (output) => {
                    string stateVariable = Context.Names.GetStateVariableName();
                    WriteCallStateExitHandler(output);
                    Context.WriteLine(output, $"{stateVariable} = {stateName};");
                    WriteCallStateEntryHandler(output);
                });
            Context.WriteLine(output, "}");
        }

        // Writes handleRaisedEvent function which handles raise statement.
        private void WriteRaisedEventHandler(StringWriter output)
        {
            string handleRaisedFunction = Context.Names.GetHandleRaisedEventFunctionName();
            string eventInterfaceName = Context.Names.GetEventInterfaceName();
            string eventArgName = Context.Names.GetEventArgumentName();
            string payloadType = Context.Names.GetDefaultPayloadTypeName();
            string payloadName = Context.Names.GetPayloadArgumentName();
            string stateVariableName = Context.Names.GetStateVariableName();
            Context.WriteLine(
                output,
                $"private void {handleRaisedFunction}({eventInterfaceName} {eventArgName}, Optional<{payloadType}> {payloadName}) {{");
            WrapInTryStmtException(
                output,
                (output) => Context.WriteLine(output, $"{eventArgName}.handle({stateVariableName}, {payloadName});"));
            Context.WriteLine(output, "}");
        }

        private void WriteFunctionArguments(StringWriter output, Function function)
        {
            BeforeSeparator separator = new BeforeSeparator(() => Context.Write(output, ", "));
            foreach (Variable argument in function.Signature.Parameters)
            {
                separator.beforeElement();
                string argumentType = Context.Names.GetJavaTypeName(argument.Type);
                string argumentName = Context.Names.GetLocalVarName(argument);
                Context.Write(output, $"{argumentType} {argumentName}");
            }
        }

        private delegate void WrapInTryStmtExceptionDelegate(StringWriter output);

        private void WrapInTryStmtException(StringWriter output, WrapInTryStmtExceptionDelegate writeCode)
        {
            Context.WriteLine(output, "try {");

            writeCode(output);

            string exceptionVariable = Context.Names.GetExceptionVariableName();
            string exceptionPayload = Context.Names.GetExceptionPayloadGetterName();
            string stateValue = Context.Names.GetGotoStmtExceptionStateGetterName();
            string eventValue = Context.Names.GetRaiseStmtExceptionEventGetterName();
            string stateExceptionClass = Context.Names.GetGotoStmtExceptionName();
            string eventExceptionClass = Context.Names.GetRaiseStmtExceptionName();
            Context.WriteLine(output, $"}} catch ({stateExceptionClass} {exceptionVariable}) {{");
            WriteCallChangeStateHandler(output,
                $"(StateBase){exceptionVariable}.{stateValue}()", $"{exceptionVariable}.{exceptionPayload}()");
            Context.WriteLine(output, $"}} catch ({eventExceptionClass} {exceptionVariable}) {{");
            WriteCallRaisedEventHandler(output,$"{exceptionVariable}.{eventValue}()", $"{exceptionVariable}.{exceptionPayload}()");
            Context.WriteLine(output, "}");
        }

        private string GetBoxedDefaultValue(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                case PrimitiveType primitiveType1 when primitiveType1.IsSameTypeAs(PrimitiveType.Int):
                case PrimitiveType primitiveType2 when primitiveType2.IsSameTypeAs(PrimitiveType.Float):
                case PrimitiveType primitiveType3 when primitiveType3.IsSameTypeAs(PrimitiveType.String):
                case EnumType enumType:
                    string unboxedDefault = GetUnboxedDefaultValue(type);
                    return $"new {Context.Names.GetJavaTypeName(type)}({unboxedDefault})";

                case PrimitiveType eventType when eventType.IsSameTypeAs(PrimitiveType.Event):
                case NamedTupleType namedTuple:
                case TupleType tupleType:
                case MapType mapType:
                case SequenceType seqType:
                case SetType setType:
                    return GetUnboxedDefaultValue(type);

                default:
                    throw new ArgumentOutOfRangeException(type.OriginalRepresentation);
            }
        }

        private string GetUnboxedDefaultValue(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "false";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "0L";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "0.0d";

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                    return "(\"\")";

                case EnumType enumType:
                {
                    string typeName = Context.Names.GetEnumTypeName(enumType.EnumDecl);
                    // TODO: Use something more reasonable here.
                    string elementName = Context.Names.GetEnumElementName(enumType.EnumDecl.Values.First());
                    return $"{typeName}.{elementName}";
                }

                case NamedTupleType namedTuple:
                {
                    List<string> quotedNames = new List<string>();
                    List<string> values = new List<string>();
                    foreach (NamedTupleEntry entry in namedTuple.Fields)
                    {
                        quotedNames.Add($"\"{Context.Names.GetTupleFieldName(entry.Name)}\"");
                        values.Add(GetBoxedDefaultValue(entry.Type));
                    }
                    string comma_names = string.Join(", ", quotedNames);
                    string comma_values = string.Join(", ", values);
                    string namedTupleClass = Context.Names.GetNamedTupleTypeName();
                    string valueInterface = Context.Names.GetValueInterfaceName();
                    return $"new {namedTupleClass}(new String[]{{{comma_names}}}, new {valueInterface}<?>[]{{{comma_values}}})";
                }

                case TupleType tupleType:
                {
                    string comma_values =
                        string.Join(", ", tupleType.Types.Select(t => GetBoxedDefaultValue(t)));
                    string tupleClass = Context.Names.GetTupleTypeName();
                    string valueInterface = Context.Names.GetValueInterfaceName();
                    return $"new {tupleClass}(new {valueInterface}<?>[]{{{comma_values}}})";
                }

                case MapType mapType:
                    return $"new {Context.Names.GetMapTypeName()}()";

                case SequenceType seqType:
                    return $"new {Context.Names.GetSeqTypeName()}()";

                case SetType setType:
                    return $"new {Context.Names.GetSetTypeName()}()";

                case PrimitiveType eventType when eventType.IsSameTypeAs(PrimitiveType.Event):
                    return "null";

                default:
                    throw new ArgumentOutOfRangeException(type.OriginalRepresentation);
            }
        }

        private static string UnOpToStr(UnaryOpType operation)
        {
            switch (operation)
            {
                case UnaryOpType.Negate:
                    return "-";

                case UnaryOpType.Not:
                    return "!";

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        private enum BoxingState
        {
            BOXED,
            UNBOXED,
            NOT_APPLICABLE,
        }

        private delegate BoxingState UnboxingDelegate(StringWriter output);

        // Unboxes the IValue object to its actual value by calling the getter function.
        private BoxingState UnboxIfNeeded(StringWriter output, UnboxingDelegate expressionWriter)
        {
            BoxingState state = expressionWriter(output);
            switch (state)
            {
                case BoxingState.BOXED:
                    Context.Write(output, ".get()");
                    return BoxingState.UNBOXED;
                default:
                    return state;
            }
        }

        // Boxes the actual value to its corresponding IValue object.
        private BoxingState BoxIfNeeded(StringWriter output, PLanguageType type, UnboxingDelegate expressionWriter)
        {
            StringWriter exprBuffer = new StringWriter();
            BoxingState state = expressionWriter(exprBuffer);
            string exprString = exprBuffer.ToString();
            switch (state)
            {
                case BoxingState.UNBOXED:
                    if (!isBoxable(type))
                    {
                        throw new InvalidStateException($"Unexpected unboxed type {type.ToString()}.");
                    }
                    Context.Write(output, $"new {Context.Names.GetJavaTypeName(type)}(");
                    Context.Write(output, exprString);
                    Context.Write(output, ")");
                    return BoxingState.UNBOXED;
                default:
                    Context.Write(output, exprString);
                    return state;
            }
        }


        private bool isBoxable(PLanguageType type)
        {
            switch (type.Canonicalize())
            {
                case DataType _:
                    throw new NotImplementedException("isBoxable is not implemented for DataType.");

                case EnumType _:
                    return true;

                case ForeignType _:
                    throw new NotImplementedException("isBoxable is not implemented for ForeignType.");

                case MapType _:
                    return false;

                case NamedTupleType _:
                    return false;

                case PermissionType _:
                    throw new NotImplementedException("isBoxable is not implemented for PermissionType.");

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    throw new NotImplementedException("isBoxable is not implemented for AnyType.");

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return true;

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return true;

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return true;

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.String):
                    return true;

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return false;

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                    throw new NotImplementedException("isBoxable is not implemented for MachineType.");

                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    throw new NotImplementedException("isBoxable is not implemented for Null.");

                case SequenceType _:
                    return false;

                case SetType _:
                    return false;

                case TupleType _:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(type.ToString());
            }
        }
 
        private static string TransformPrintMessage(string message)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == '\'')
                {
                    sb.Append("''");
                }
                else if (message[i] == '{')
                {
                    if (i + 1 == message.Length)
                    {
                        throw new ArgumentException("unmatched opening brace", nameof(message));
                    }

                    if (message[i + 1] == '{')
                    {
                        // handle "{{"
                        i++;
                        sb.Append("'{'");
                    }
                    else if (char.IsDigit(message[i + 1]))
                    {
                        sb.Append("{");
                        while (++i < message.Length && '0' <= message[i] && message[i] <= '9')
                        {
                            sb.Append(message[i]);
                        }

                        if (i == message.Length || message[i] != '}')
                        {
                            throw new ArgumentException("unmatched opening brace in position expression",
                                nameof(message));
                        }
                        sb.Append("}");
                    }
                    else
                    {
                        throw new ArgumentException("opening brace not followed by digits", nameof(message));
                    }
                }
                else if (message[i] == '}')
                {
                    if (i + 1 == message.Length || message[i + 1] != '}')
                    {
                        throw new ArgumentException("unmatched closing brace", nameof(message));
                    }

                    // handle "}}"
                    sb.Append("'}'");
                    i++;
                }
                else
                {
                    sb.Append(message[i]);
                }
            }

            return sb.ToString();
        }

        private BoxingState BoxedOrNotApplicable(PLanguageType type)
        {
            if (isBoxable(type))
            {
                return BoxingState.BOXED;
            }
            else
            {
                return BoxingState.NOT_APPLICABLE;
            }
        }

        private class InvalidStateException : Exception
        {
            public InvalidStateException(string reason) : base(reason)
            {
            }
        }
    }
}
