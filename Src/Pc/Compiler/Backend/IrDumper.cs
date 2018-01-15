using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend
{
    public class IrDumper
    {
        private readonly StringBuilder writer = new StringBuilder();
        private int depth;
        private string padding;

        private IrDumper() { }

        public static string Dump(Scope scope)
        {
            var dumper = new IrDumper();
            foreach (IPDecl tree in scope.AllDecls)
            {
                dumper.WalkTree(tree);
            }

            return dumper.writer.ToString();
        }

        private void Indent() { padding = new string(' ', ++depth * 4); }

        private void Dedent() { padding = new string(' ', --depth * 4); }
        
        private void WriteParts(string part) { writer.Append(part); }

        private void WriteParts(params string[] parts)
        {
            foreach (string part in parts)
            {
                writer.Append(part);
            }
        }

        private void WriteParts(params object[] parts)
        {
            foreach (object part in parts)
            {
                switch (part)
                {
                    case IPExpr expr:
                        DumpExpr(expr);
                        break;
                    case IEnumerable<IPExpr> exprs:
                        JoinExprs(exprs);
                        break;
                    case IEnumerable<string> strs:
                        JoinObjects(strs);
                        break;
                    case IPDecl decl:
                        writer.Append(decl.Name);
                        break;
                    case PLanguageType type:
                        writer.Append(type.OriginalRepresentation);
                        break;
                    default:
                        writer.Append(part);
                        break;
                }
            }
        }

        private void WriteStmt(params object[] parts)
        {
            WriteParts(padding);
            WriteParts(parts);
            WriteParts(Environment.NewLine);
        }

        private void WalkTree(IPAST tree)
        {
            switch (tree)
            {
                case null:
                    throw new ArgumentNullException(nameof(tree));
                case Function function:
                    if (string.IsNullOrEmpty(function.Name))
                    {
                        return;
                    }

                    WriteStmt("fun ",
                              function,
                              "(",
                              DumpParams(function.Signature.Parameters),
                              ") : ",
                              function.Signature.ReturnType);
                    WriteStmt("{");
                    Indent();
                    foreach (Variable localVariable in function.LocalVariables)
                    {
                        WalkTree(localVariable);
                    }

                    WalkTree(function.Body);
                    Dedent();
                    WriteStmt("}");
                    break;
                case Interface @interface:
                    WriteStmt("interface ",
                              @interface,
                              "(",
                              @interface.PayloadType,
                              ") receives ",
                              DumpEventSet(@interface.ReceivableEvents),
                              ";");
                    break;
                case Machine machine:
                    WriteStmt(machine.IsSpec ? "spec " : "",
                              "machine ",
                              machine,
                              " : ",
                              string.Join(", ", machine.Interfaces.Select(x => x.Name)));
                    WriteStmt("  assert ", machine.Assert, " assume ", machine.Assume);
                    WriteStmt("  receives ", DumpEventSet(machine.Receives));
                    WriteStmt("  sends ", DumpEventSet(machine.Sends));
                    if (machine.IsSpec)
                    {
                        WriteStmt("  observes ", DumpEventSet(machine.Observes));
                    }

                    WriteStmt("{");
                    Indent();
                    foreach (Variable machineField in machine.Fields)
                    {
                        WalkTree(machineField);
                    }

                    foreach (Function machineMethod in machine.Methods)
                    {
                        WalkTree(machineMethod);
                    }

                    foreach (StateGroup machineGroup in machine.Groups)
                    {
                        WalkTree(machineGroup);
                    }

                    foreach (State machineState in machine.States)
                    {
                        WalkTree(machineState);
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
                              pEnum.Values.SelectMany(x => new object[] {x.Name, " = ", x.Value}),
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
                    WriteStmt(assignStmt.Variable, " = ", assignStmt.Value, "; //plain assignment");
                    break;
                case CompoundStmt compoundStmt:
                    foreach (IPStmt stmt in compoundStmt.Statements)
                    {
                        WalkTree(stmt);
                    }

                    break;
                case CtorStmt ctorStmt:
                    WriteStmt("new ", ctorStmt.Machine, "(", ctorStmt.Arguments, ");");
                    break;
                case FunCallStmt funCallStmt:
                    WriteStmt(funCallStmt.Fun, "(", funCallStmt.ArgsList, ");");
                    break;
                case GotoStmt gotoStmt:
                    WriteStmt("goto ", gotoStmt.State, ", ", gotoStmt.Payload, ";");
                    break;
                case IfStmt ifStmt:
                    WriteStmt("if (", ifStmt.Condition, ")");
                    WriteStmt("{");
                    Indent();
                    WalkTree(ifStmt.ThenBranch);
                    Dedent();
                    WriteStmt("}");
                    WriteStmt("else");
                    WriteStmt("{");
                    Indent();
                    WalkTree(ifStmt.ElseBranch);
                    Dedent();
                    WriteStmt("}");
                    break;
                case InsertStmt insertStmt:
                    WriteStmt(insertStmt.Variable, " += (", insertStmt.Index, ", ", insertStmt.Value, ");");
                    break;
                case MoveAssignStmt moveAssignStmt:
                    WriteStmt(moveAssignStmt.ToLocation, " = ", moveAssignStmt.FromVariable, " move;");
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
                    foreach (var recvCase in receiveStmt.Cases)
                    {
                        WriteStmt("case ",
                                  recvCase.Key,
                                  " : (",
                                  DumpParams(recvCase.Value.Signature.Parameters),
                                  ") {");
                        Indent();
                        WalkTree(recvCase.Value.Body);
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
                case SendStmt sendStmt:
                    WriteStmt("send ", sendStmt.MachineExpr, ", ", sendStmt.Evt, ", ", sendStmt.ArgsList, ";");
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
                    WalkTree(whileStmt.Body);
                    Dedent();
                    WriteStmt("}");
                    break;
                case EventDefer eventDefer:
                    WriteStmt("defer ", eventDefer.Trigger, ";");
                    break;
                case EventDoAction eventDoAction:
                    WriteParts(padding, "on ", eventDoAction.Trigger, " do ");
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
                    string temp = state.Temperature.Equals(StateTemperature.COLD) ? "cold " :
                                  state.Temperature.Equals(StateTemperature.HOT) ? "hot " : "warm ";
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
                        WalkTree(state.Entry?.Body ?? new NoStmt(state.SourceLocation));
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
                        WalkTree(state.Exit?.Body ?? new NoStmt(state.SourceLocation));
                        Dedent();
                        WriteStmt("}");
                    }

                    foreach (var handler in state.AllEventHandlers)
                    {
                        WalkTree(handler.Value);
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
                        WalkTree(subGroup);
                    }

                    foreach (State state in stateGroup.States)
                    {
                        WalkTree(state);
                    }

                    Dedent();
                    WriteStmt("}");
                    break;
                case EnumElem _:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tree));
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
                          DumpParams(target.Signature.Parameters),
                          ") : ",
                          target.Signature.ReturnType,
                          " {");
                writer.AppendLine();
                Indent();
                WalkTree(target.Body);
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
            var actualSep = "";
            foreach (IPExpr item in items)
            {
                writer.Append(actualSep);
                writer.Append(item);
                actualSep = ", ";
            }

            if (actualSep == "")
            {
                writer.Append("<<null>>");
            }
        }

        private void JoinExprs(IEnumerable<IPExpr> items)
        {
            var actualSep = "";
            foreach (IPExpr item in items)
            {
                writer.Append(actualSep);
                DumpExpr(item);
                actualSep = ", ";
            }

            if (actualSep == "")
            {
                writer.Append("<<null>>");
            }
        }

        private void DumpExpr(IPExpr expr)
        {
            switch (expr)
            {
                case null:
                    WriteParts("<<null>>");
                    break;
                case BinOpExpr binOpExpr:
                    WriteParts("(", binOpExpr.Lhs, ") ", DumpBinOp(binOpExpr.Operation), " (", binOpExpr.Rhs, ")");
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
                case ContainsKeyExpr containsKeyExpr:
                    WriteParts("(", containsKeyExpr.Key, ") in (", containsKeyExpr.Map, ")");
                    break;
                case CtorExpr ctorExpr:
                    WriteParts("new ", ctorExpr.Machine, "(", ctorExpr.Arguments, ")");
                    break;
                case DefaultExpr defaultExpr:
                    WriteParts("default(", defaultExpr.Type, ")");
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    WriteParts(enumElemRefExpr.EnumElem);
                    break;
                case EventRefExpr eventRefExpr:
                    WriteParts(eventRefExpr.PEvent);
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
                    var ntType = (NamedTupleType) namedTupleExpr.Type;
                    WriteParts("(");
                    var ntSep = "";
                    for (var i = 0; i < ntType.Fields.Count; i++)
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
                    WriteParts(DumpUnOp(unaryOpExpr.Operation), "(", unaryOpExpr.SubExpr, ")");
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

        private string DumpUnOp(UnaryOpType operation)
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

        private string DumpBinOp(BinOpType operation)
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

        private string DumpEventSet(IEventSet eventSet)
        {
            switch (eventSet)
            {
                case EventSet eventSet1:
                    return $"{{ {string.Join(", ", eventSet1.Events.Select(x => x.Name))} }}";
                case NamedEventSet namedEventSet:
                    return namedEventSet.Name;
                case null:
                case UniversalEventSet _:
                    return "<all>";
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventSet));
            }
        }

        private static string DumpParams(IEnumerable<Variable> parameters)
        {
            return string.Join(", ", parameters.Select(v => $"{v.Name}: {v.Type.OriginalRepresentation}"));
        }
    }
}

