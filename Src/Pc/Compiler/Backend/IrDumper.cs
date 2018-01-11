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

        private void WriteLine(string line)
        {
            writer.Append(padding);
            writer.AppendLine(line);
        }

        private void WriteStmt(params object[] parts)
        {
            writer.Append(padding);
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

            writer.AppendLine();
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
                    WriteLine("{");
                    Indent();
                    foreach (Variable localVariable in function.LocalVariables)
                    {
                        WalkTree(localVariable);
                    }

                    WalkTree(function.Body);
                    Dedent();
                    WriteLine("}");
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
                    WriteLine($"  assert {machine.Assert} assume {machine.Assume}");
                    WriteLine($"  receives {DumpEventSet(machine.Receives)}");
                    WriteLine($"  sends {DumpEventSet(machine.Sends)}");
                    if (machine.IsSpec)
                    {
                        WriteLine($"  observes {DumpEventSet(machine.Observes)}");
                    }

                    WriteLine("{");
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
                    WriteLine("}");
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
                              string.Join(", ", pEnum.Values.Select(x => $"{x.Name} = {x.Value}")),
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
                              pEvent.PayloadType);
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
                    WriteLine("{");
                    Indent();
                    WalkTree(ifStmt.ThenBranch);
                    Dedent();
                    WriteLine("}");
                    WriteLine("else");
                    WriteLine("{");
                    Indent();
                    WalkTree(ifStmt.ElseBranch);
                    Dedent();
                    WriteLine("}");
                    break;
                case InsertStmt insertStmt:
                    WriteStmt(insertStmt.Variable, " += (", insertStmt.Index, ", ", insertStmt.Value, ");");
                    break;
                case MoveAssignStmt moveAssignStmt:
                    WriteStmt(moveAssignStmt.ToLocation, " = ", moveAssignStmt.FromVariable, " move;");
                    break;
                case NoStmt _:
                    WriteLine("; // no action");
                    break;
                case PopStmt _:
                    WriteLine("pop;");
                    break;
                case PrintStmt printStmt:
                    WriteStmt("print \"", printStmt.Message, "\", ", printStmt.Args, ";");
                    break;
                case RaiseStmt raiseStmt:
                    WriteStmt("raise ", raiseStmt.PEvent, ", ", raiseStmt.Payload, ";");
                    break;
                case ReceiveStmt receiveStmt:
                    WriteLine("receive {");
                    Indent();
                    foreach (var recvCase in receiveStmt.Cases)
                    {
                        WriteLine($"case {recvCase.Key.Name} : ({DumpParams(recvCase.Value.Signature.Parameters)}) {{");
                        Indent();
                        WalkTree(recvCase.Value.Body);
                        Dedent();
                        WriteLine("}");
                    }

                    Dedent();
                    WriteLine("}");
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
                    WriteLine("{");
                    Indent();
                    WalkTree(whileStmt.Body);
                    Dedent();
                    WriteLine("}");
                    break;
                case EventDefer eventDefer:
                    WriteLine($"defer {eventDefer.Trigger.Name};");
                    break;
                case EventDoAction eventDoAction:
                    WriteLine($"on {eventDoAction.Trigger.Name} do ");
                    PrintFunctionRef(eventDoAction.Target);
                    break;
                case EventGotoState eventGotoState:
                    WriteStmt("on ", eventGotoState.Trigger, " goto ", eventGotoState.Target, " with ");
                    PrintFunctionRef(eventGotoState.TransitionFunction);
                    break;
                case EventIgnore eventIgnore:
                    WriteLine($"ignore {eventIgnore.Trigger.Name};");
                    break;
                case EventPushState eventPushState:
                    WriteLine($"on {eventPushState.Trigger.Name} push {eventPushState.Target.Name};");
                    break;
                case State state:
                    string temp = state.Temperature.Equals(StateTemperature.COLD) ? "cold" :
                                  state.Temperature.Equals(StateTemperature.HOT) ? "hot" : "/* warm */";
                    WriteLine($"{(state.IsStart ? "start " : "")}{temp} state {state.Name}");
                    WriteLine("{");
                    Indent();
                    if (!string.IsNullOrEmpty(state.Entry?.Name))
                    {
                        WriteLine($"entry {state.Entry.Name};");
                    }
                    else
                    {
                        WriteLine("entry");
                        WriteLine("{");
                        Indent();
                        WalkTree(state.Entry?.Body ?? new NoStmt(state.SourceLocation));
                        Dedent();
                        WriteLine("}");
                    }

                    if (state.Exit?.Name != null)
                    {
                        WriteLine($"exit {state.Exit.Name};");
                    }
                    else
                    {
                        WriteLine("exit");
                        WriteLine("{");
                        Indent();
                        WalkTree(state.Exit?.Body ?? new NoStmt(state.SourceLocation));
                        Dedent();
                        WriteLine("}");
                    }

                    foreach (var handler in state.AllEventHandlers)
                    {
                        WalkTree(handler.Value);
                    }

                    Dedent();
                    WriteLine("}");
                    break;
                case StateGroup stateGroup:
                    WriteLine($"group {stateGroup.Name}");
                    WriteLine("{");
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
                    WriteLine("}");
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
                WriteLine("  <<null>>");
                return;
            }

            if (string.IsNullOrEmpty(target.Name))
            {
                WalkTree(target);
            }
            else
            {
                WriteLine($"  {target.Name}");
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
                    writer.Append("<<null>>");
                    break;
                case BinOpExpr binOpExpr:
                    writer.Append("(");
                    DumpExpr(binOpExpr.Lhs);
                    writer.Append($") {DumpBinOp(binOpExpr.Operation)} (");
                    DumpExpr(binOpExpr.Rhs);
                    writer.Append(")");
                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    writer.Append(boolLiteralExpr.Value.ToString());
                    break;
                case CastExpr castExpr:
                    writer.Append("(");
                    DumpExpr(castExpr.SubExpr);
                    writer.Append($") as {castExpr.Type.OriginalRepresentation}");
                    break;
                case CoerceExpr coerceExpr:
                    writer.Append("(");
                    DumpExpr(coerceExpr.SubExpr);
                    writer.Append($") to {coerceExpr.Type.OriginalRepresentation}");
                    break;
                case ContainsKeyExpr containsKeyExpr:
                    writer.Append("(");
                    DumpExpr(containsKeyExpr.Key);
                    writer.Append(") in (");
                    DumpExpr(containsKeyExpr.Map);
                    writer.Append(")");
                    break;
                case CtorExpr ctorExpr:
                    writer.Append($"new {ctorExpr.Machine.Name}(");
                    JoinExprs(ctorExpr.Arguments);
                    writer.Append(")");
                    break;
                case DefaultExpr defaultExpr:
                    writer.Append($"default({defaultExpr.Type.OriginalRepresentation})");
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    writer.Append(enumElemRefExpr.EnumElem.Name);
                    break;
                case EventRefExpr eventRefExpr:
                    writer.Append(eventRefExpr.PEvent.Name);
                    break;
                case FairNondetExpr _:
                    writer.Append("$$");
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    writer.Append(floatLiteralExpr.Value.ToString(CultureInfo.InvariantCulture));
                    break;
                case FunCallExpr funCallExpr:
                    writer.Append(funCallExpr.Function.Name);
                    writer.Append("(");
                    JoinExprs(funCallExpr.Arguments);
                    writer.Append(")");
                    break;
                case IntLiteralExpr intLiteralExpr:
                    writer.Append(intLiteralExpr.Value.ToString());
                    break;
                case KeysExpr keysExpr:
                    writer.Append("keys(");
                    DumpExpr(keysExpr.Expr);
                    writer.Append(")");
                    break;
                case LinearAccessRefExpr linearAccessRefExpr:
                    writer.Append(linearAccessRefExpr.Variable.Name);
                    writer.Append(linearAccessRefExpr.LinearType.Equals(LinearType.Move) ? " move" : " swap");
                    break;
                case MapAccessExpr mapAccessExpr:
                    writer.Append("(");
                    DumpExpr(mapAccessExpr.MapExpr);
                    writer.Append(")[");
                    DumpExpr(mapAccessExpr.IndexExpr);
                    writer.Append("]");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    writer.Append("(");
                    DumpExpr(namedTupleAccessExpr.SubExpr);
                    writer.Append($").");
                    writer.Append(namedTupleAccessExpr.FieldName);
                    break;
                case NamedTupleExpr namedTupleExpr:
                    var ntType = (NamedTupleType) namedTupleExpr.Type;
                    writer.Append("(");
                    var ntSep = "";
                    for (var i = 0; i < ntType.Fields.Count; i++)
                    {
                        writer.Append(ntSep);
                        writer.Append(ntType.Fields[i].Name);
                        writer.Append(" = ");
                        DumpExpr(namedTupleExpr.TupleFields[i]);

                        ntSep = ", ";
                    }

                    writer.Append(")");
                    break;
                case NondetExpr _:
                    writer.Append("$");
                    break;
                case NullLiteralExpr _:
                    writer.Append("null");
                    break;
                case SeqAccessExpr seqAccessExpr:
                    writer.Append("(");
                    DumpExpr(seqAccessExpr.SeqExpr);
                    writer.Append(")[");
                    DumpExpr(seqAccessExpr.IndexExpr);
                    writer.Append("]");
                    break;
                case SizeofExpr sizeofExpr:
                    writer.Append("sizeof(");
                    DumpExpr(sizeofExpr.Expr);
                    writer.Append(")");
                    break;
                case ThisRefExpr _:
                    writer.Append("this");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    writer.Append("(");
                    DumpExpr(tupleAccessExpr.SubExpr);
                    writer.Append(".");
                    writer.Append(tupleAccessExpr.FieldNo.ToString());
                    break;
                case UnaryOpExpr unaryOpExpr:
                    writer.Append(DumpUnOp(unaryOpExpr.Operation));
                    writer.Append("(");
                    DumpExpr(unaryOpExpr.SubExpr);
                    writer.Append(")");
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    writer.Append("(");
                    JoinExprs(unnamedTupleExpr.TupleFields);
                    writer.Append(")");
                    break;
                case ValuesExpr valuesExpr:
                    writer.Append("values(");
                    DumpExpr(valuesExpr.Expr);   
                    writer.Append(")");
                    break;
                case VariableAccessExpr variableAccessExpr:
                    writer.Append(variableAccessExpr.Variable.Name);
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
