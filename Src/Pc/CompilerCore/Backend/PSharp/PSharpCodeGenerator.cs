using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc.Backend.ASTExt;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.PSharp
{
    public class PSharpCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            CompiledFile csharpSource = GenerateSource(context, globalScope);
            return new List<CompiledFile> {csharpSource};
        }

        private CompiledFile GenerateSource(CompilationContext context, Scope globalScope)
        {
            var source = new CompiledFile(context.FileName);

            WriteSourcePrologue(context, source.Stream);

            foreach (IPDecl decl in globalScope.AllDecls)
            {
                WriteDecl(context, source.Stream, decl);
            }

            // TODO: generate tuple type classes.

            WriteSourceEpilogue(context, source.Stream);

            return source;
        }

        private void WriteSourcePrologue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "using Microsoft.PSharp;");
            context.WriteLine(output, "using System;");
            context.WriteLine(output, "using System.Collections.Generic;");
            context.WriteLine(output, "using System.IO;");
            context.WriteLine(output);
            context.WriteLine(output, $"namespace {context.ProjectName}");
            context.WriteLine(output, "{");
            context.WriteLine(output, $"public static partial class {context.GlobalFunctionClassName} {{}}");
        }

        private void WriteSourceEpilogue(CompilationContext context, StringWriter output)
        {
            context.WriteLine(output, "}");
        }

        private void WriteDecl(CompilationContext context, StringWriter output, IPDecl decl)
        {
            string declName = context.Names.GetNameForDecl(decl);
            switch (decl)
            {
                case Function function:
                    context.WriteLine(output, $"public static partial class {context.GlobalFunctionClassName}");
                    context.WriteLine(output, "{");
                    WriteFunction(context, output, function);
                    context.WriteLine(output, "}");
                    break;
                case PEvent pEvent:
                    if (!pEvent.IsBuiltIn)
                    {
                        context.WriteLine(output, $"internal class {declName} : Event");
                        context.WriteLine(output, "{");
                        WriteEvent(context, output, pEvent);
                        context.WriteLine(output, "}");
                    }
                    break;
                case Machine machine:
                    context.WriteLine(output, $"internal class {declName} : Machine");
                    context.WriteLine(output, "{");
                    WriteMachine(context, output, machine);
                    context.WriteLine(output, "}");
                    break;
                case PEnum pEnum:
                    context.WriteLine(output, $"public enum {declName}");
                    context.WriteLine(output, "{");
                    foreach (EnumElem enumElem in pEnum.Values)
                    {
                        context.WriteLine(output, $"{declName} = {enumElem.Value},");
                    }

                    context.WriteLine(output, "}");
                    break;
                default:
                    context.WriteLine(output, $"// TODO: {decl.GetType().Name} {declName}");
                    break;
            }
        }

        private void WriteEvent(CompilationContext context, StringWriter output, PEvent pEvent)
        {
            // generate the payload type
            if (!pEvent.PayloadType.IsSameTypeAs(PrimitiveType.Null))
            {
                string payloadType = GetCSharpType(context, pEvent.PayloadType);
                context.WriteLine(output, $"public {payloadType} payload;");
            }

            // add a constructor to initialize the assert and assume fields
            context.WriteLine(output, $"public {pEvent.Name} (): base({pEvent.Assert}, {pEvent.Assume});" + "{ }");
        }

        private void WriteMachine(CompilationContext context, StringWriter output, Machine machine)
        {
            foreach (Variable field in machine.Fields)
            {
                context.WriteLine(output, $"private {GetCSharpType(context, field.Type)} {context.Names.GetNameForDecl(field)} = {GetDefaultValue(context, field.Type)}");
            }

            foreach (Function method in machine.Methods)
            {
                WriteFunction(context, output, method);
            }

            foreach (State state in machine.States)
            {
                if (state.IsStart)
                {
                    context.WriteLine(output, "[Start]");
                }

                if (state.Entry != null)
                {
                    context.WriteLine(output, $"[OnEntry(nameof({context.Names.GetNameForDecl(state.Entry)}))]");
                }

                var deferredEvents = new List<string>();
                var ignoredEvents = new List<string>();
                foreach (var eventHandler in state.AllEventHandlers)
                {
                    PEvent pEvent = eventHandler.Key;
                    IStateAction stateAction = eventHandler.Value;
                    switch (stateAction)
                    {
                        case EventDefer _:
                            deferredEvents.Add($"typeof({context.Names.GetNameForDecl(pEvent)})");
                            break;
                        case EventDoAction eventDoAction:
                            context.WriteLine(
                                output,
                                $"[OnEventDoAction(typeof({context.Names.GetNameForDecl(pEvent)}), nameof({context.Names.GetNameForDecl(eventDoAction.Target)}))]");
                            break;
                        case EventGotoState eventGotoState when eventGotoState.TransitionFunction == null:
                            context.WriteLine(
                                output,
                                $"[OnEventGotoState(typeof({context.Names.GetNameForDecl(pEvent)}), typeof({context.Names.GetNameForDecl(eventGotoState.Target)}))]");
                            break;
                        case EventGotoState eventGotoState when eventGotoState.TransitionFunction != null:
                            context.WriteLine(
                                output,
                                $"[OnEventGotoState(typeof({context.Names.GetNameForDecl(pEvent)}), typeof({context.Names.GetNameForDecl(eventGotoState.Target)}), nameof({context.Names.GetNameForDecl(eventGotoState.TransitionFunction)}))]");
                            break;
                        case EventIgnore _:
                            ignoredEvents.Add($"typeof({context.Names.GetNameForDecl(pEvent)})");
                            break;
                        case EventPushState eventPushState:
                            context.WriteLine(
                                output,
                                $"[OnEventPushState(typeof({context.Names.GetNameForDecl(pEvent)}), typeof({context.Names.GetNameForDecl(eventPushState.Target)}))]");
                            break;
                    }
                }

                if (deferredEvents.Count > 0)
                {
                    context.WriteLine(output, $"[DeferEvents({string.Join(", ", deferredEvents.AsEnumerable())})]");
                }

                if (ignoredEvents.Count > 0)
                {
                    context.WriteLine(output, $"[IgnoreEvents({string.Join(", ", ignoredEvents.AsEnumerable())})]");
                }

                if (state.Exit != null)
                {
                    context.WriteLine(output, $"[OnExit(nameof({context.Names.GetNameForDecl(state.Exit)}))]");
                }

                context.WriteLine(output, $"class {context.Names.GetNameForDecl(state)} : MachineState");
                context.WriteLine(output, "{");
                context.WriteLine(output, "}");
            }
        }

        private void WriteFunction(CompilationContext context, StringWriter output, Function function)
        {
            bool isStatic = function.Owner == null;
            FunctionSignature signature = function.Signature;

            string staticKeyword = isStatic ? "static " : "";
            string returnType = GetCSharpType(context, signature.ReturnType);
            string functionName = context.Names.GetNameForDecl(function);
            string functionParameters =
                string.Join(
                    ", ",
                    signature.Parameters.Select(param => $"{GetCSharpType(context, param.Type)} {context.Names.GetNameForDecl(param)}"));

            context.WriteLine(output, $"public {staticKeyword}{returnType} {functionName}({functionParameters})");
            WriteFunctionBody(context, output, function);
        }

        private void WriteFunctionBody(CompilationContext context, StringWriter output, Function function)
        {
            context.WriteLine(output, "{");
            foreach (Variable local in function.LocalVariables)
            {
                PLanguageType type = local.Type;
                context.WriteLine(output, $"{GetCSharpType(context, type)} {context.Names.GetNameForDecl(local)} = {GetDefaultValue(context, type)};");
            }
            foreach (IPStmt bodyStatement in function.Body.Statements)
            {
                WriteStmt(context, output, bodyStatement);
            }
            context.WriteLine(output, "}");
        }

        private void WriteStmt(CompilationContext context, StringWriter output, IPStmt stmt)
        {
            switch (stmt)
            {
                case AnnounceStmt announceStmt:
                    break;
                case AssertStmt assertStmt:
                    context.Write(output, "this.Assert(");
                    WriteExpr(context, output, assertStmt.Assertion);
                    context.Write(output, ",");
                    context.Write(output, $"\"{assertStmt.Message}\"");
                    break;
                case AssignStmt assignStmt:
                    WriteLValue(context, output, assignStmt.Location);
                    context.Write(output, " = ");
                    WriteExpr(context, output, assignStmt.Value);
                    context.WriteLine(output, ";");
                    break;
                case CompoundStmt compoundStmt:
                    context.WriteLine(output, "{");
                    foreach (IPStmt subStmt in compoundStmt.Statements)
                    {
                        WriteStmt(context, output, subStmt);
                    }

                    context.WriteLine(output, "}");
                    break;
                case CtorStmt ctorStmt:
                    break;
                case FunCallStmt funCallStmt:
                    break;
                case GotoStmt gotoStmt:
                    break;
                case IfStmt ifStmt:
                    context.Write(output, "if (");
                    WriteExpr(context, output, ifStmt.Condition);
                    context.WriteLine(output, ")");
                    WriteStmt(context, output, ifStmt.ThenBranch);
                    if (ifStmt.ElseBranch != null)
                    {
                        context.WriteLine(output, "else");
                        WriteStmt(context, output, ifStmt.ElseBranch);
                    }
                    break;
                case InsertStmt insertStmt:
                    break;
                case MoveAssignStmt moveAssignStmt:
                    WriteLValue(context, output, moveAssignStmt.ToLocation);
                    context.WriteLine(output, $" = {context.Names.GetNameForDecl(moveAssignStmt.FromVariable)};");
                    break;
                case NoStmt _:
                    break;
                case PopStmt popStmt:
                    break;
                case PrintStmt printStmt:
                    context.Write(output, $"runtime.WriteLine(\"{printStmt.Message}\"");
                    foreach (IPExpr printArg in printStmt.Args)
                    {
                        context.Write(output, ", ");
                        WriteExpr(context, output, printArg);
                    }

                    context.WriteLine(output, ");");
                    break;
                case RaiseStmt raiseStmt:
                    break;
                case ReceiveStmt receiveStmt:
                    break;
                case RemoveStmt removeStmt:
                    break;
                case ReturnStmt returnStmt:
                    context.Write(output, "return ");
                    WriteExpr(context, output, returnStmt.ReturnValue);
                    context.WriteLine(output, ";");
                    break;
                case SendStmt sendStmt:
                    break;
                case SwapAssignStmt swapAssignStmt:
                    break;
                case WhileStmt whileStmt:
                    context.Write(output, "while (");
                    WriteExpr(context, output, whileStmt.Condition);
                    context.WriteLine(output, ")");
                    WriteStmt(context, output, whileStmt.Body);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stmt));
            }
        }

        private void WriteLValue(CompilationContext context, StringWriter output, IPExpr lvalue)
        {
            switch (lvalue)
            {
                case MapAccessExpr mapAccessExpr:
                    context.Write(output, "(");
                    WriteLValue(context, output, mapAccessExpr.MapExpr);
                    context.Write(output, ")[");
                    WriteExpr(context, output, mapAccessExpr.IndexExpr);
                    context.Write(output, "]");
                    break;
                case NamedTupleAccessExpr namedTupleAccessExpr:
                    throw new NotImplementedException();
                case SeqAccessExpr seqAccessExpr:
                    context.Write(output, "(");
                    WriteLValue(context, output, seqAccessExpr.SeqExpr);
                    context.Write(output, ")[");
                    WriteExpr(context, output, seqAccessExpr.IndexExpr);
                    context.Write(output, "]");
                    break;
                case TupleAccessExpr tupleAccessExpr:
                    throw new NotImplementedException();
                case VariableAccessExpr variableAccessExpr:
                    context.Write(output, context.Names.GetNameForDecl(variableAccessExpr.Variable));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lvalue));
            }
        }

        private void WriteExpr(CompilationContext context, StringWriter output, IPExpr pExpr)
        {
            switch (pExpr)
            {
                case CloneExpr cloneExpr:
                    WriteClone(context, output, cloneExpr.Term);
                    break;
                case BinOpExpr binOpExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, binOpExpr.Lhs);
                    context.Write(output, $") {BinOpToStr(binOpExpr.Operation)} (");
                    WriteExpr(context, output, binOpExpr.Rhs);
                    context.Write(output, ")");
                    break;
                case BoolLiteralExpr boolLiteralExpr:
                    context.Write(output, boolLiteralExpr.Value ? "true" : "false");
                    break;
                case CastExpr castExpr:
                    throw new NotImplementedException();
                case CoerceExpr coerceExpr:
                    throw new NotImplementedException();
                case ContainsKeyExpr containsKeyExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, containsKeyExpr.Map);
                    context.Write(output, ").ContainsKey(");
                    WriteExpr(context, output, containsKeyExpr.Key);
                    context.Write(output, ")");
                    break;
                case CtorExpr ctorExpr:
                    break;
                case DefaultExpr defaultExpr:
                    context.Write(output, GetDefaultValue(context, defaultExpr.Type));
                    break;
                case EnumElemRefExpr enumElemRefExpr:
                    EnumElem enumElem = enumElemRefExpr.Value;
                    context.Write(output, $"{context.Names.GetNameForDecl(enumElem.ParentEnum)}.{context.Names.GetNameForDecl(enumElem)}");
                    break;
                case EventRefExpr eventRefExpr:
                    context.Write(output, $"new {context.Names.GetNameForDecl(eventRefExpr.Value)}()");
                    break;
                case FairNondetExpr _:
                    context.Write(output, "this.FairRandom()");
                    break;
                case FloatLiteralExpr floatLiteralExpr:
                    context.Write(output, $"{floatLiteralExpr.Value}");
                    break;
                case FunCallExpr funCallExpr:
                    break;
                case IntLiteralExpr intLiteralExpr:
                    context.Write(output, $"{intLiteralExpr.Value}");
                    break;
                case KeysExpr keysExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, keysExpr.Expr);
                    context.Write(output, ").Keys.ToList()");
                    break;
                case LinearAccessRefExpr linearAccessRefExpr:
                    string swapKeyword = linearAccessRefExpr.LinearType.Equals(LinearType.Swap) ? "ref " : "";
                    context.Write(output, $"{swapKeyword}{context.Names.GetNameForDecl(linearAccessRefExpr.Variable)}");
                    break;
                case NamedTupleExpr namedTupleExpr:
                    throw new NotImplementedException();
                case NondetExpr _:
                    context.Write(output, "this.Random()");
                    break;
                case NullLiteralExpr _:
                    context.Write(output, "null");
                    break;
                case SizeofExpr sizeofExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, sizeofExpr.Expr);
                    context.Write(output, ").Count");
                    break;
                case ThisRefExpr _:
                    context.Write(output, "this");
                    break;
                case UnaryOpExpr unaryOpExpr:
                    context.Write(output, $"{UnOpToStr(unaryOpExpr.Operation)}(");
                    WriteExpr(context, output, unaryOpExpr.SubExpr);
                    context.Write(output, ")");
                    break;
                case UnnamedTupleExpr unnamedTupleExpr:
                    throw new NotImplementedException();
                case ValuesExpr valuesExpr:
                    context.Write(output, "(");
                    WriteExpr(context, output, valuesExpr.Expr);
                    context.Write(output, ").Values.ToList()");
                    break;
                case MapAccessExpr _:
                case NamedTupleAccessExpr _:
                case SeqAccessExpr _:
                case TupleAccessExpr _:
                case VariableAccessExpr _:
                    WriteLValue(context, output, pExpr);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pExpr));
            }
        }

        private void WriteClone(CompilationContext context, StringWriter output, IExprTerm cloneExprTerm)
        {
            if (!(cloneExprTerm is IVariableRef variableRef))
            {
                WriteExpr(context, output, cloneExprTerm);
                return;
            }
            
            var variable = variableRef.Variable;
            context.Write(output, RenderClone(context, variable.Type, context.Names.GetNameForDecl(variable)));
        }

        private string RenderClone(CompilationContext context, PLanguageType cloneType, string termName)
        {
            switch (cloneType.Canonicalize())
            {
                case SequenceType seq:
                    var elem = context.Names.GetTemporaryName("elem");
                    return $"({termName}).ConvertAll({elem} => {RenderClone(context, seq.ElementType, elem)})";
                case MapType map:
                    var key = context.Names.GetTemporaryName("k");
                    var val = context.Names.GetTemporaryName("v");
                    return $"({termName}).ToDictionary({key} => {RenderClone(context, map.KeyType, key + ".Key")}, {val} => {RenderClone(context, map.ValueType, val + ".Value")})";
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Int):
                    return termName;
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Float):
                    return termName;
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Bool):
                    return termName;
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Machine):
                    return termName;
                case PrimitiveType type when type.IsSameTypeAs(PrimitiveType.Event):
                    return GetDefaultValue(context, type);
                default:
                    throw new NotImplementedException($"Cloning {cloneType.OriginalRepresentation}");
            }
        }

        private string GetCSharpType(CompilationContext context, PLanguageType returnType)
        {
            switch (returnType.Canonicalize())
            {
                case BoundedType _:
                    return "object";
                case EnumType enumType:
                    return context.Names.GetNameForDecl(enumType.EnumDecl);
                case ForeignType _:
                    throw new NotImplementedException();
                case MapType mapType:
                    return $"Dictionary<{GetCSharpType(context, mapType.KeyType)}, {GetCSharpType(context, mapType.ValueType)}>";
                case NamedTupleType _:
                    throw new NotImplementedException();
                case PermissionType _:
                    return "Permissions";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Any):
                    return "object";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "bool";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "int";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "double";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Event):
                    return "Event";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Machine):
                    return "Permissions";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Null):
                    return "void";
                case SequenceType sequenceType:
                    return $"List<{GetCSharpType(context, sequenceType.ElementType)}>";
                case TupleType _:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(returnType));
            }
        }

        private string GetDefaultValue(CompilationContext context, PLanguageType returnType)
        {
            switch (returnType.Canonicalize())
            {
                case EnumType enumType:
                    return $"({context.Names.GetNameForDecl(enumType.EnumDecl)})(0)";
                case MapType mapType:
                    return $"new {GetCSharpType(context, mapType)}()";
                case SequenceType sequenceType:
                    return $"new <{GetCSharpType(context, sequenceType)}>()";
                case NamedTupleType _:
                    throw new NotImplementedException();
                case TupleType _:
                    throw new NotImplementedException();
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Bool):
                    return "false";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Int):
                    return "0";
                case PrimitiveType primitiveType when primitiveType.IsSameTypeAs(PrimitiveType.Float):
                    return "0.0";
                case PermissionType _:
                case PrimitiveType anyType when anyType.IsSameTypeAs(PrimitiveType.Any):
                case PrimitiveType eventType when eventType.IsSameTypeAs(PrimitiveType.Event):
                case PrimitiveType machineType when machineType.IsSameTypeAs(PrimitiveType.Machine):
                case ForeignType _:
                case BoundedType _:
                    return "null";
                default:
                    throw new ArgumentOutOfRangeException(nameof(returnType));
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

        private static string BinOpToStr(BinOpType binOpType)
        {
            switch (binOpType)
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
                    throw new ArgumentOutOfRangeException(nameof(binOpType), binOpType, null);
            }
        }
    }
}
