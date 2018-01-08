using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private readonly StringWriter writer = new StringWriter();
        private int depth = 0;
        private string padding;

        public static string Dump(Scope scope)
        {
            var dumper = new IrDumper();
            foreach (var tree in scope.AllDecls)
            {
                dumper.WalkTree(tree);
            }
            return dumper.writer.ToString();
        }

        private IrDumper()
        {
        }

        private void Indent() { padding = new string(' ', ++depth * 4); }

        private void Dedent() { padding = new string(' ', --depth * 4); }

        private void WriteLine(string line)
        {
            writer.WriteLine($"{this.padding}{line.TrimEnd()}");
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

                    WriteLine($"fun {function.Name}({DumpParams(function.Signature.Parameters)}) : {function.Signature.ReturnType.OriginalRepresentation}");
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
                    WriteLine($"interface {@interface.Name}({@interface.PayloadType.OriginalRepresentation}) receives {DumpEventSet(@interface.ReceivableEvents)};");
                    break;
                case Machine machine:
                    WriteLine($"{(machine.IsSpec ? "spec " : "")}machine {machine.Name} : {string.Join(", ", machine.Interfaces.Select(x => x.Name))}");
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
                    WriteLine($"eventset {namedEventSet.Name} = {{ {string.Join(", ", namedEventSet.Events.Select(x => x.Name))} }};");
                    break;
                case PEnum pEnum:
                    WriteLine($"enum {pEnum.Name} = {{ {string.Join(", ", pEnum.Values.Select(x => $"{x.Name} = {x.Value}"))} }};");
                    break;
                case PEvent pEvent:
                    WriteLine($"event {pEvent.Name} assert {pEvent.Assert} assume {pEvent.Assume} : {pEvent.PayloadType.OriginalRepresentation};");
                    break;
                case TypeDef typeDef:
                    WriteLine($"type {typeDef.Name} = {typeDef.Type.OriginalRepresentation}");
                    break;
                case Variable variable:
                    WriteLine($"var {variable.Name} : {variable.Type.OriginalRepresentation};");
                    break;
                case AnnounceStmt announceStmt:
                    WriteLine($"announce {DumpExpr(announceStmt.PEvent)}, {DumpExpr(announceStmt.Payload)};");
                    break;
                case AssertStmt assertStmt:
                    WriteLine($"assert {DumpExpr(assertStmt.Assertion)}, \"{assertStmt.Message}\";");
                    break;
                case AssignStmt assignStmt:
                    WriteLine($"{DumpExpr(assignStmt.Variable)} = {DumpExpr(assignStmt.Value)}; // plain assignment");
                    break;
                case CompoundStmt compoundStmt:
                    foreach (IPStmt stmt in compoundStmt.Statements)
                    {
                        WalkTree(stmt);
                    }
                    break;
                case CtorStmt ctorStmt:
                    WriteLine($"new {ctorStmt.Machine.Name}({string.Join(", ", ctorStmt.Arguments.Select(DumpExpr))});");
                    break;
                case FunCallStmt funCallStmt:
                    WriteLine($"{funCallStmt.Fun.Name}({string.Join(", ", funCallStmt.ArgsList.Select(DumpExpr))});");
                    break;
                case GotoStmt gotoStmt:
                    WriteLine($"goto {gotoStmt.State.Name}, {DumpExpr(gotoStmt.Payload)};");
                    break;
                case IfStmt ifStmt:
                    WriteLine($"if ({DumpExpr(ifStmt.Condition)})");
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
                    WriteLine($"{DumpExpr(insertStmt.Variable)} += ({DumpExpr(insertStmt.Index)}, {DumpExpr(insertStmt.Value)});");
                    break;
                case MoveAssignStmt moveAssignStmt:
                    WriteLine($"{DumpExpr(moveAssignStmt.ToLocation)} = {moveAssignStmt.FromVariable.Name} move;");
                    break;
                case NoStmt _:
                    WriteLine("; // no action");
                    break;
                case PopStmt _:
                    WriteLine("pop;");
                    break;
                case PrintStmt printStmt:
                    WriteLine($"print \"{printStmt.Message}\", {string.Join(", ", printStmt.Args.Select(DumpExpr))};");
                    break;
                case RaiseStmt raiseStmt:
                    WriteLine($"raise {DumpExpr(raiseStmt.PEvent)}, {string.Join(", ", raiseStmt.Payload.Select(DumpExpr))};");
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
                    WriteLine($"{DumpExpr(removeStmt.Variable)} -= {DumpExpr(removeStmt.Value)};");
                    break;
                case ReturnStmt returnStmt:
                    WriteLine($"return {DumpExpr(returnStmt.ReturnValue)};");
                    break;
                case SendStmt sendStmt:
                    WriteLine($"send {DumpExpr(sendStmt.MachineExpr)}, {DumpExpr(sendStmt.Evt)}, {string.Join(", ", sendStmt.ArgsList.Select(DumpExpr))};");
                    break;
                case SwapAssignStmt swapAssignStmt:
                    WriteLine($"{DumpExpr(swapAssignStmt.NewLocation)} = {swapAssignStmt.OldLocation.Name} swap; // swap assign");
                    break;
                case WhileStmt whileStmt:
                    WriteLine($"while ({DumpExpr(whileStmt.Condition)})");
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
                    WriteLine($"on {eventGotoState.Trigger.Name} goto {eventGotoState.Target.Name} with ");
                    PrintFunctionRef(eventGotoState.TransitionFunction);
                    break;
                case EventIgnore eventIgnore:
                    WriteLine($"ignore {eventIgnore.Trigger.Name};");
                    break;
                case EventPushState eventPushState:
                    WriteLine($"on {eventPushState.Trigger.Name} push {eventPushState.Target.Name};");
                    break;
                case State state:
                    var temp = state.Temperature.Equals(StateTemperature.COLD) ? "cold" :
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
                    foreach (var subGroup in stateGroup.Groups)
                    {
                        WalkTree(subGroup);
                    }
                    foreach (var state in stateGroup.States)
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
                WriteLine($"  <<null>>");
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

        private string DumpExpr(IPExpr expr)
        {
            switch (expr)
            {
                case null:
                    return "<<null>>";
                case BinOpExpr binOpExpr:
                    return $"({DumpExpr(binOpExpr.Lhs)}) {DumpBinOp(binOpExpr.Operation)} ({DumpExpr(binOpExpr.Rhs)})";
                case BoolLiteralExpr boolLiteralExpr:
                    return $"{boolLiteralExpr.Value}";
                case CastExpr castExpr:
                    return $"({DumpExpr(castExpr.SubExpr)}) as {castExpr.Type.OriginalRepresentation}";
                case CoerceExpr coerceExpr:
                    return $"({DumpExpr(coerceExpr.SubExpr)}) to {coerceExpr.Type.OriginalRepresentation}";
                case ContainsKeyExpr containsKeyExpr:
                    return $"({DumpExpr(containsKeyExpr.Key)}) in ({DumpExpr(containsKeyExpr.Map)})";
                case CtorExpr ctorExpr:
                    return $"new {ctorExpr.Machine.Name}({string.Join(", ", ctorExpr.Arguments.Select(DumpExpr))})";
                case DefaultExpr defaultExpr:
                    return $"default({defaultExpr.Type.OriginalRepresentation})";
                case EnumElemRefExpr enumElemRefExpr:
                    return enumElemRefExpr.EnumElem.Name;
                case EventRefExpr eventRefExpr:
                    return eventRefExpr.PEvent.Name;
                case FairNondetExpr _:
                    return "$$";
                case FloatLiteralExpr floatLiteralExpr:
                    return floatLiteralExpr.Value.ToString(CultureInfo.InvariantCulture);
                case FunCallExpr funCallExpr:
                    return $"{funCallExpr.Function.Name}({string.Join(", ", funCallExpr.Arguments.Select(DumpExpr))})";
                case IntLiteralExpr intLiteralExpr:
                    return intLiteralExpr.Value.ToString();
                case KeysExpr keysExpr:
                    return $"keys({DumpExpr(keysExpr.Expr)})";
                case LinearAccessRefExpr linearAccessRefExpr:
                    return linearAccessRefExpr.Variable.Name + (linearAccessRefExpr.LinearType.Equals(LinearType.Move) ? " move" : " swap");
                case MapAccessExpr mapAccessExpr:
                    return $"({DumpExpr(mapAccessExpr.MapExpr)})[{DumpExpr(mapAccessExpr.IndexExpr)}]";
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    return $"({DumpExpr(namedTupleAccessExpr.SubExpr)}).{namedTupleAccessExpr.FieldName}";
                case NamedTupleExpr namedTupleExpr:
                    var ntType = (NamedTupleType)namedTupleExpr.Type;
                    return $"({string.Join(", ", ntType.Names.Zip(namedTupleExpr.TupleFields, (name, pExpr) => $"{name} = {DumpExpr(pExpr)}"))}";
                case NondetExpr _:
                    return "$";
                case NullLiteralExpr _:
                    return "null";
                case SeqAccessExpr seqAccessExpr:
                    return $"({DumpExpr(seqAccessExpr.SeqExpr)})[{DumpExpr(seqAccessExpr.IndexExpr)}]";
                case SizeofExpr sizeofExpr:
                    return $"sizeof({DumpExpr(sizeofExpr.Expr)})";
                case ThisRefExpr _:
                    return "this";
                case TupleAccessExpr tupleAccessExpr:
                    return $"({DumpExpr(tupleAccessExpr.SubExpr)}).{tupleAccessExpr.FieldNo}";
                case UnaryOpExpr unaryOpExpr:
                    return $"{DumpUnOp(unaryOpExpr.Operation)}({DumpExpr(unaryOpExpr.SubExpr)})";
                case UnnamedTupleExpr unnamedTupleExpr:
                    return $"({string.Join(", ", unnamedTupleExpr.TupleFields.Select(DumpExpr))})";
                case ValuesExpr valuesExpr:
                    return $"values({DumpExpr(valuesExpr.Expr)})";
                case VariableAccessExpr variableAccessExpr:
                    return variableAccessExpr.Variable.Name;
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

        private string DumpBinOp(BinOpType operation) {
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