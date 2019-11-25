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
using System.Globalization;
using System.Linq;

namespace Plang.Compiler.Backend.Debugging
{
    public class IrToPseudoP : IrRenderer
    {
        private IrToPseudoP()
        {
        }

        public static string Dump(Scope scope)
        {
            IrToPseudoP dumper = new IrToPseudoP();
            return dumper.Render(scope);
        }

        protected override void WriteDecl(IPDecl decl)
        {
            WriteTree(decl);
        }

        protected override void WriteTypeRef(PLanguageType type)
        {
            WriteParts(type.OriginalRepresentation);
        }

        protected override void WriteDeclRef(IPDecl decl)
        {
            string name = decl.Name;
            if (decl is State state)
            {
                name = state.QualifiedName;
            }

            WriteParts(name);
        }

        protected override void WriteStringList(IEnumerable<string> strs)
        {
            JoinObjects(strs);
        }

        private void WriteStmt(params object[] parts)
        {
            WriteParts(Padding);
            WriteParts(parts);
            WriteParts(Environment.NewLine);
        }

        private void WriteTree(IPAST tree)
        {
            switch (tree)
            {
                case Function function:
                    if (string.IsNullOrEmpty(function.Name))
                    {
                        return;
                    }

                    WriteStmt("fun ",
                        function,
                        "(",
                        WriteParams(function.Signature.Parameters),
                        ") : ",
                        function.Signature.ReturnType);
                    WriteStmt("{");
                    Indent();
                    foreach (Variable localVariable in function.LocalVariables)
                    {
                        WriteTree(localVariable);
                    }

                    WriteTree(function.Body);
                    Dedent();
                    WriteStmt("}");
                    break;

                case Interface @interface:
                    WriteStmt("interface ",
                        @interface,
                        "(",
                        @interface.PayloadType,
                        ") receives ",
                        WriteEventSet(@interface.ReceivableEvents),
                        ";");
                    break;

                case Machine machine:
                    WriteStmt(machine.IsSpec ? "spec " : "",
                        "machine ",
                        machine);
                    string machineAssume = machine.Assume?.ToString() ?? "max";
                    string machineAssert = machine.Assert?.ToString() ?? "max";
                    WriteStmt("  assert ", machineAssert, " assume ", machineAssume);
                    WriteStmt("  receives ", WriteEventSet(machine.Receives));
                    WriteStmt("  sends ", WriteEventSet(machine.Sends));
                    if (machine.IsSpec)
                    {
                        WriteStmt("  observes ", WriteEventSet(machine.Observes));
                    }

                    WriteStmt("{");
                    Indent();
                    foreach (Variable machineField in machine.Fields)
                    {
                        WriteTree(machineField);
                    }

                    foreach (Function machineMethod in machine.Methods)
                    {
                        WriteTree(machineMethod);
                    }

                    foreach (StateGroup machineGroup in machine.Groups)
                    {
                        WriteTree(machineGroup);
                    }

                    foreach (State machineState in machine.States)
                    {
                        WriteTree(machineState);
                    }

                    Dedent();
                    WriteStmt("}");
                    break;

                case NamedEventSet namedEventSet:
                    WriteStmt("eventset ",
                        namedEventSet,
                        " = { ",
                        string.Join(", ", namedEventSet.Events.Select(x => x.Name)),
                        " };");
                    break;

                case PEnum pEnum:
                    WriteStmt("enum ",
                        pEnum,
                        " = { ",
                        pEnum.Values.Select(x => $"{x.Name} = {x.Value}"),
                        " };");
                    break;

                case PEvent pEvent:
                    WriteStmt("event ",
                        pEvent,
                        " assert ",
                        pEvent.Assert,
                        " assume ",
                        pEvent.Assume,
                        " : ",
                        pEvent.PayloadType,
                        ";");
                    break;

                case TypeDef typeDef:
                    WriteStmt("type ", typeDef, " = ", typeDef.Type, ";");
                    break;

                case Variable variable:
                    WriteStmt("var ", variable, " : ", variable.Type, ";");
                    break;

                case AnnounceStmt announceStmt:
                    WriteStmt("announce ", announceStmt.PEvent, ", ", announceStmt.Payload, ";");
                    break;

                case AssertStmt assertStmt:
                    WriteStmt("assert ", assertStmt.Assertion, ", \"", assertStmt.Message, "\";");
                    break;

                case AssignStmt assignStmt:
                    WriteStmt(assignStmt.Location, " = ", assignStmt.Value, ";");
                    break;

                case CompoundStmt compoundStmt:
                    foreach (IPStmt stmt in compoundStmt.Statements)
                    {
                        WriteTree(stmt);
                    }

                    break;

                case CtorStmt ctorStmt:
                    WriteStmt("new ", ctorStmt.Interface, "(", ctorStmt.Arguments, ");");
                    break;

                case FunCallStmt funCallStmt:
                    WriteStmt(funCallStmt.Function, "(", funCallStmt.ArgsList, ");");
                    break;

                case GotoStmt gotoStmt:
                    WriteStmt("goto ", gotoStmt.State, ", ", gotoStmt.Payload, ";");
                    break;

                case IfStmt ifStmt:
                    WriteStmt("if (", ifStmt.Condition, ")");
                    WriteStmt("{");
                    Indent();
                    WriteTree(ifStmt.ThenBranch);
                    Dedent();
                    WriteStmt("}");
                    WriteStmt("else");
                    WriteStmt("{");
                    Indent();
                    WriteTree(ifStmt.ElseBranch);
                    Dedent();
                    WriteStmt("}");
                    break;

                case AddStmt addStmt:
                    WriteStmt(addStmt.Variable, " += (", addStmt.Value, ");");
                    break;

                case InsertStmt insertStmt:
                    WriteStmt(insertStmt.Variable, " += (", insertStmt.Index, ", ", insertStmt.Value, ");");
                    break;

                case MoveAssignStmt moveAssignStmt:
                    WriteStmt(moveAssignStmt.ToLocation, " <- ", moveAssignStmt.FromVariable, " move;");
                    break;

                case NoStmt _:
                    WriteStmt("; // no action");
                    break;

                case PopStmt _:
                    WriteStmt("pop;");
                    break;

                case PrintStmt printStmt:
                    WriteStmt("print \"", printStmt.Message, "\", ", printStmt.Args, ";");
                    break;

                case RaiseStmt raiseStmt:
                    WriteStmt("raise ", raiseStmt.PEvent, ", ", raiseStmt.Payload, ";");
                    break;

                case ReceiveStmt receiveStmt:
                    WriteStmt("receive {");
                    Indent();
                    foreach (KeyValuePair<PEvent, Function> recvCase in receiveStmt.Cases)
                    {
                        WriteStmt("case ",
                            recvCase.Key,
                            " : (",
                            WriteParams(recvCase.Value.Signature.Parameters),
                            ") {");
                        Indent();
                        WriteTree(recvCase.Value.Body);
                        Dedent();
                        WriteStmt("}");
                    }

                    Dedent();
                    WriteStmt("}");
                    break;

                case RemoveStmt removeStmt:
                    WriteStmt(removeStmt.Variable, " -= ", removeStmt.Value, ";");
                    break;

                case ReturnStmt returnStmt:
                    WriteStmt("return ", returnStmt.ReturnValue, ";");
                    break;

                case BreakStmt breakStmt:
                    WriteStmt("break;");
                    break;

                case ContinueStmt continueStmt:
                    WriteStmt("continue;");
                    break;

                case SendStmt sendStmt:
                    WriteStmt("send ", sendStmt.MachineExpr, ", ", sendStmt.Evt, ", ", sendStmt.Arguments, ";");
                    break;

                case StringAssignStmt stringAssignStmt:
                    WriteStmt(stringAssignStmt.SourceLocation, " = ", "\"", stringAssignStmt.BaseString, "\", ", stringAssignStmt.Args, ";");
                    break;

                case SwapAssignStmt swapAssignStmt:
                    WriteStmt(swapAssignStmt.NewLocation,
                        " = ",
                        swapAssignStmt.OldLocation,
                        " swap; //swap assign");
                    break;

                case WhileStmt whileStmt:
                    WriteStmt("while (", whileStmt.Condition, ")");
                    WriteStmt("{");
                    Indent();
                    WriteTree(whileStmt.Body);
                    Dedent();
                    WriteStmt("}");
                    break;

                case EventDefer eventDefer:
                    WriteStmt("defer ", eventDefer.Trigger, ";");
                    break;

                case EventDoAction eventDoAction:
                    WriteParts(Padding, "on ", eventDoAction.Trigger, " do ");
                    PrintFunctionRef(eventDoAction.Target);
                    break;

                case EventGotoState eventGotoState:
                    WriteStmt("on ", eventGotoState.Trigger, " goto ", eventGotoState.Target, " with ");
                    PrintFunctionRef(eventGotoState.TransitionFunction);
                    break;

                case EventIgnore eventIgnore:
                    WriteStmt("ignore ", eventIgnore.Trigger, ";");
                    break;

                case EventPushState eventPushState:
                    WriteStmt("on ", eventPushState.Trigger, " push ", eventPushState.Target, ";");
                    break;

                case State state:
                    string start = state.IsStart ? "start " : "";
                    string temp = state.Temperature.Equals(StateTemperature.Cold) ? "cold " :
                        state.Temperature.Equals(StateTemperature.Hot) ? "hot " : "warm ";
                    WriteStmt(start, temp, "state ", state);
                    WriteStmt("{");
                    Indent();
                    if (!string.IsNullOrEmpty(state.Entry?.Name))
                    {
                        WriteStmt("entry ", state.Entry, ";");
                    }
                    else
                    {
                        WriteStmt("entry");
                        WriteStmt("{");
                        Indent();
                        if (state.Entry is Function stateEntry)
                        {
                            foreach (Variable localVariable in stateEntry.LocalVariables)
                            {
                                WriteTree(localVariable);
                            }

                            WriteTree(stateEntry.Body);
                        }
                        else
                        {
                            WriteTree(new NoStmt(state.SourceLocation));
                        }

                        Dedent();
                        WriteStmt("}");
                    }

                    if (!string.IsNullOrEmpty(state.Exit?.Name))
                    {
                        WriteStmt("exit ", state.Exit, ";");
                    }
                    else
                    {
                        WriteStmt("exit");
                        WriteStmt("{");
                        Indent();
                        if (state.Exit is Function stateExit)
                        {
                            foreach (Variable localVariable in stateExit.LocalVariables)
                            {
                                WriteTree(localVariable);
                            }

                            WriteTree(stateExit.Body);
                        }
                        else
                        {
                            WriteTree(new NoStmt(state.SourceLocation));
                        }

                        Dedent();
                        WriteStmt("}");
                    }

                    foreach (KeyValuePair<PEvent, IStateAction> handler in state.AllEventHandlers)
                    {
                        WriteTree(handler.Value);
                    }

                    Dedent();
                    WriteStmt("}");
                    break;

                case StateGroup stateGroup:
                    WriteStmt("group ", stateGroup);
                    WriteStmt("{");
                    Indent();
                    foreach (StateGroup subGroup in stateGroup.Groups)
                    {
                        WriteTree(subGroup);
                    }

                    foreach (State state in stateGroup.States)
                    {
                        WriteTree(state);
                    }

                    Dedent();
                    WriteStmt("}");
                    break;

                case EnumElem _:
                    break;

                default:
                    WriteStmt($"// UNKNOWN declaration {tree.GetType().FullName}");
                    break;
            }
        }

        private void PrintFunctionRef(Function target)
        {
            if (target == null)
            {
                WriteStmt("  <<null>>");
                return;
            }

            if (string.IsNullOrEmpty(target.Name))
            {
                WriteParts("(",
                    WriteParams(target.Signature.Parameters),
                    ") : ",
                    target.Signature.ReturnType,
                    " {",
                    Environment.NewLine);
                Indent();
                WriteTree(target.Body);
                Dedent();
                WriteStmt("}");
            }
            else
            {
                WriteStmt(target.Name);
            }
        }

        private void JoinObjects(IEnumerable<object> items)
        {
            string actualSep = "";
            foreach (object item in items)
            {
                WriteParts(actualSep);
                WriteParts(item);
                actualSep = ", ";
            }

            if (actualSep == "")
            {
                WriteParts("<<null>>");
            }
        }

        protected override void WriteExprList(IEnumerable<IPExpr> items)
        {
            string actualSep = "";
            foreach (IPExpr item in items)
            {
                WriteParts(actualSep);
                WriteExpr(item);
                actualSep = ", ";
            }

            if (actualSep == "")
            {
                WriteParts("<<null>>");
            }
        }

        protected override void WriteExpr(IPExpr expr)
        {
            switch (expr)
            {
                case null:
                    WriteParts("<<null>>");
                    break;

                case BinOpExpr binOpExpr:
                    WriteParts("(", binOpExpr.Lhs, ") ", WriteBinOp(binOpExpr.Operation), " (", binOpExpr.Rhs, ")");
                    break;

                case BoolLiteralExpr boolLiteralExpr:
                    WriteParts(boolLiteralExpr.Value);
                    break;

                case CastExpr castExpr:
                    WriteParts("(", castExpr.SubExpr, ") as ", castExpr.Type);
                    break;

                case CoerceExpr coerceExpr:
                    WriteParts("(", coerceExpr.SubExpr, ") to ", coerceExpr.Type);
                    break;

                case ContainsExpr containsKeyExpr:
                    WriteParts("(", containsKeyExpr.Item, ") in (", containsKeyExpr.Collection, ")");
                    break;

                case CloneExpr cloneExpr:
                    WriteParts("$Clone(", cloneExpr.Term, ")");
                    break;

                case CtorExpr ctorExpr:
                    WriteParts("new ", ctorExpr.Interface, "(", ctorExpr.Arguments, ")");
                    break;

                case DefaultExpr defaultExpr:
                    WriteParts("default(", defaultExpr.Type, ")");
                    break;

                case EnumElemRefExpr enumElemRefExpr:
                    WriteParts(enumElemRefExpr.Value);
                    break;

                case EventRefExpr eventRefExpr:
                    WriteParts(eventRefExpr.Value);
                    break;

                case FairNondetExpr _:
                    WriteParts("$$");
                    break;

                case FloatLiteralExpr floatLiteralExpr:
                    WriteParts(floatLiteralExpr.Value.ToString(CultureInfo.InvariantCulture));
                    break;

                case FunCallExpr funCallExpr:
                    WriteParts(funCallExpr.Function, "(", funCallExpr.Arguments, ")");
                    break;

                case IntLiteralExpr intLiteralExpr:
                    WriteParts(intLiteralExpr.Value.ToString());
                    break;

                case KeysExpr keysExpr:
                    WriteParts("keys(", keysExpr.Expr, ")");
                    break;

                case LinearAccessRefExpr linearAccessRefExpr:
                    WriteParts(linearAccessRefExpr.Variable.Name,
                        linearAccessRefExpr.LinearType.Equals(LinearType.Move) ? " move" : " swap");
                    break;

                case MapAccessExpr mapAccessExpr:
                    WriteParts("(", mapAccessExpr.MapExpr, ")[", mapAccessExpr.IndexExpr, "]");
                    break;

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    WriteParts("(", namedTupleAccessExpr.SubExpr, ").", namedTupleAccessExpr.FieldName);
                    break;

                case NamedTupleExpr namedTupleExpr:
                    NamedTupleType ntType = (NamedTupleType)namedTupleExpr.Type;
                    WriteParts("(");
                    string ntSep = "";
                    for (int i = 0; i < ntType.Fields.Count; i++)
                    {
                        WriteParts(ntSep, ntType.Fields[i].Name, " = ", namedTupleExpr.TupleFields[i]);
                        ntSep = ", ";
                    }

                    WriteParts(")");
                    break;

                case NondetExpr _:
                    WriteParts("$");
                    break;

                case NullLiteralExpr _:
                    WriteParts("null");
                    break;

                case SeqAccessExpr seqAccessExpr:
                    WriteParts("(", seqAccessExpr.SeqExpr, ")[", seqAccessExpr.IndexExpr, "]");
                    break;

                case SizeofExpr sizeofExpr:
                    WriteParts("sizeof(", sizeofExpr.Expr, ")");
                    break;

                case ThisRefExpr _:
                    WriteParts("this");
                    break;

                case TupleAccessExpr tupleAccessExpr:
                    WriteParts("(", tupleAccessExpr.SubExpr, ").", tupleAccessExpr.FieldNo);
                    break;

                case UnaryOpExpr unaryOpExpr:
                    WriteParts(WriteUnOp(unaryOpExpr.Operation), "(", unaryOpExpr.SubExpr, ")");
                    break;

                case UnnamedTupleExpr unnamedTupleExpr:
                    WriteParts("(", unnamedTupleExpr.TupleFields, ")");
                    break;

                case ValuesExpr valuesExpr:
                    WriteParts("values(", valuesExpr.Expr, ")");
                    break;

                case VariableAccessExpr variableAccessExpr:
                    WriteParts(variableAccessExpr.Variable.Name);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(expr));
            }
        }

        private string WriteUnOp(UnaryOpType operation)
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

        private string WriteBinOp(BinOpType operation)
        {
            switch (operation)
            {
                case BinOpType.Add:
                    return "+";

                case BinOpType.Sub:
                    return "-";

                case BinOpType.Mul:
                    return "*";

                case BinOpType.Div:
                    return "/";

                case BinOpType.Eq:
                    return "==";

                case BinOpType.Neq:
                    return "!=";

                case BinOpType.Lt:
                    return "<";

                case BinOpType.Le:
                    return "<=";

                case BinOpType.Gt:
                    return ">";

                case BinOpType.Ge:
                    return ">=";

                case BinOpType.And:
                    return "&&";

                case BinOpType.Or:
                    return "||";

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        private string WriteEventSet(IEventSet eventSet)
        {
            switch (eventSet)
            {
                case EventSet eventSet1:
                    return $"{{ {string.Join(", ", eventSet1.Events.Select(x => x.Name))} }}";

                case NamedEventSet namedEventSet:
                    return namedEventSet.Name;

                case null:
                    return "<all>";

                default:
                    throw new ArgumentOutOfRangeException(nameof(eventSet));
            }
        }

        private static string WriteParams(IEnumerable<Variable> parameters)
        {
            return string.Join(", ", parameters.Select(v => $"{v.Name}: {v.Type.OriginalRepresentation}"));
        }
    }
}