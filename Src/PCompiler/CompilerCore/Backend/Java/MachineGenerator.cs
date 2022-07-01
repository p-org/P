using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java {

    internal class MachineGenerator : JavaSourceGenerator
    {

        private Machine _currentMachine; // Some generated code is machine-dependent, so stash the current machine here.

        internal MachineGenerator(string filename) : base(filename)
        {
        }

        /// <summary>
        /// Generates Java code for a given compilation job's machine and monitor definitions.
        ///
        /// Currently, we should be able to use nested classes to put everything we need in a single
        /// Java file, in a manner similar to how the C# extractor uses namespaces.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected override void GenerateCodeImpl()
        {
            if (GlobalScope.Typedefs.Any())
            {
                foreach (var t in GlobalScope.Typedefs)
                {
                    if (t.Type is ForeignType foreignType)
                    {
                        WriteForeignType(foreignType);
                    }
                }
                WriteLine();
            }

            WriteLine($"public class {Constants.MachineNamespaceName} {{");

            //TODO: Do specs need interfaces?

            foreach (var m in GlobalScope.Machines)
            {
                _currentMachine = m;
                if (m.IsSpec)
                {
                    WriteMonitorDecl();
                }
                else
                {
                    WriteMachineDecl();
                }
                _currentMachine = null;
            }
            WriteLine("}");
        }


        private void WriteForeignType(ForeignType ft)
        {
            WriteLine($"import PForeign.globals.{ft.CanonicalRepresentation};");
        }


        private void WriteMachineDecl()
        {
            WriteLine($"// PMachine {_currentMachine.Name} elided ");
        }

        private void WriteMonitorDecl()
        {
            string cname = Names.GetNameForDecl(_currentMachine);

            WriteLine($"public static class {cname} extends prt.Monitor {{");

            // monitor fields
            foreach (var field in _currentMachine.Fields)
            {
                TypeManager.JType type = Types.JavaTypeFor(field.Type);
                string name = Names.GetNameForDecl(field);

                WriteLine($"private {type.TypeName} {name} = {type.DefaultValue};");
                WriteLine($"public {type.TypeName} get_{name}() {{ return this.{name}; }};");
                WriteLine();
            }
            WriteLine();

            // state identifiers
            foreach (var s in _currentMachine.States)
            {
                //TODO: I think it's fine to use unqualified names here.  But, confirm.
                WriteLine($"public String {Names.IdentForState(s)} = \"{s.Name}\";");
            }
            WriteLine();

            // constructor
            WriteMonitorCstr();
            WriteLine();

            // .getEventTypes()
            WriteEventsAccessor();
            WriteLine();

            // functions
            foreach (var f in _currentMachine.Methods)
            {
                WriteFunction(f);
            }
            WriteLine();

            WriteLine($"}} // {cname} monitor definition");
        }


        private void WriteFunction(Function f)
        {
            if (f.IsForeign)
            {
                return;
            }

            if (f.CanReceive == true)
            {
                WriteLine($"// Async function {f.Name} elided");
                return;
            }

            WriteFunctionSignature(f); WriteLine(" {");

            if (f.LocalVariables.Any())
            {
                foreach (var decl in f.LocalVariables)
                {
                    //TODO: for reference types the default value can simply be null; it will be reassigned later.
                    TypeManager.JType t = Types.JavaTypeFor(decl.Type);
                    WriteLine($"{t.TypeName} {Names.GetNameForDecl(decl)} = {t.DefaultValue};");
                }
                WriteLine();
            }

            foreach (var stmt in f.Body.Statements)
            {
                WriteStmt(stmt);
            }

            WriteLine("}");
        }

        private void WriteFunctionSignature(Function f)
        {
            string fname = Names.GetNameForDecl(f);

            Write("private ");

            bool isStatic = f.Owner == null;
            if (isStatic)
            {
                Write("static ");
            }


            string args;
            if (f.IsAnon)
            {
                if (f.Signature.Parameters.Count == 0)
                {
                    args = "";
                }
                else if (f.Signature.Parameters.Count == 1)
                {
                    TypeManager.JType t = Types.JavaTypeFor(f.Signature.ParameterTypes.First());
                    string argname = Names.GetNameForDecl(f.Signature.Parameters.First());
                    args = $"{t.TypeName} {argname}";
                }
                else
                {
                    string file = f.SourceLocation.start.TokenSource.SourceName;
                    int line = f.SourceLocation.start.Line;
                    throw new Exception(
                        $"Function beginning at {file}:{line} has unexpected number {f.Signature.Parameters.Count} of arguments");
                }
            }
            else
            {
                args = string.Join(
                    ",",
                    f.Signature.Parameters.Select(v =>
                        $"{Types.JavaTypeFor(v.Type).TypeName} {Names.GetNameForDecl(v)}"));
            }

            TypeManager.JType retType = Types.JavaTypeFor(f.Signature.ReturnType);

            Write($"{retType.TypeName} {fname}({args})");

            // If this function has exceptional control flow (for raising events or state transition)
            // we need to annotate it appropriately.
            List<string> throwables = new List<string>();
            if (f.CanChangeState == true)
            {
                throwables.Add("prt.TransitionException");
            }
            if (f.CanRaiseEvent == true)
            {
                throwables.Add("prt.RaiseEventException");
            }
            if (throwables.Count > 0)
            {
                Write($"throws {string.Join(", ", throwables)}");
            }

        }

        private void WriteMonitorCstr()
        {
            string cname = Names.GetNameForDecl(_currentMachine);

            WriteLine($"public {cname}() {{");
            WriteLine("super();");

            foreach (var s in _currentMachine.States)
            {
                WriteStateBuilderDecl(s);
            }
            WriteLine("} // constructor");
        }

        private void WriteEventsAccessor()
        {
            WriteLine("public java.util.List<Class<? extends prt.events.PEvent<?>>> getEventTypes() {");
            Write("return java.util.Arrays.asList(");
            foreach (var (sep, ev) in _currentMachine.Observes.Events.WithPrefixSep(", "))
            {
                Write($"{sep}{Constants.EventNamespaceName}.{ev.Name}.class");
            }
            WriteLine(");");
            WriteLine("}");
        }

        private void WriteStateBuilderDecl(State s)
        {
            WriteLine($"addState(new prt.State.Builder({Names.IdentForState(s)})");
            WriteLine($".isInitialState({TypeManager.JType.JBool.ToJavaLiteral(s.IsStart)})");

            if (s.Entry != null)
            {
                WriteStateBuilderEntryHandler(s.Entry);
            }
            foreach (var (e, a) in s.AllEventHandlers)
            {
                WriteStateBuilderEventHandler(e, a);
            }
            if (s.Exit != null)
            {
                WriteStateBuilderExitHandler(s.Exit);
            }

            WriteLine(".build());");
        }

        private void WriteStateBuilderEntryHandler(Function f)
        {
            string fname = Names.GetNameForDecl(f);
            WriteLine($".withEntry(this::{fname})");
        }

        private void WriteStateBuilderEventHandler(PEvent e, IStateAction a)
        {
            string ename = $"{Constants.EventNamespaceName}.{Names.GetNameForDecl(e)}";

            switch (a)
            {
                case EventDefer _:
                    WriteLine($"// Ignoring deferred event {ename}");
                    break;
                case EventDoAction da when da.Target.Signature.Parameters.Count == 0:
                {
                    string aname = Names.GetNameForDecl(da.Target);
                    WriteLine($".withEvent({ename}.class, __ -> {aname}())");
                    break;
                }
                case EventDoAction da when da.Target.Signature.Parameters.Count > 0:
                {
                    string aname = Names.GetNameForDecl(da.Target);
                    WriteLine($".withEvent({ename}.class, this::{aname})");
                    break;
                }
                case EventGotoState { TransitionFunction: null } gs:
                {
                    string sname = Names.IdentForState(gs.Target);
                    WriteLine($".withEvent({ename}.class, __ -> gotoState({sname}))");
                    break;
                }
                case EventGotoState { TransitionFunction: { } } gs:
                {
                    string sname = Names.IdentForState(gs.Target);
                    string tname = Names.GetNameForDecl(gs.TransitionFunction);
                    int argcount = gs.TransitionFunction.Signature.ParameterTypes.Count();

                    switch (argcount)
                    {
                        case 0:
                            WriteLine($".withEvent({ename}.class, __ -> {{ {tname}(); gotoState({sname}); }})");
                            break;
                        case 1:
                            WriteLine($".withEvent({ename}.class, p -> {{ {tname}(p); gotoState({sname}); }})");
                            break;
                        default:
                            throw new Exception($"Unexpected {argcount}-arity for event handler for {ename}");
                    }
                    break;
                }
                case EventIgnore _:
                    WriteLine($".withEvent({ename}.class, __ -> {{ ; }})");
                    break;
                default:
                    throw new NotImplementedException($"TODO: {a.GetType()} not implemented.");
            }
        }

        private void WriteStateBuilderExitHandler(Function f)
        {
            string fname = Names.GetNameForDecl(f);
            WriteLine($".withExit(this::{fname})");
        }

        private void WriteStmt(IPStmt stmt)
        {
            TypeManager.JType t;

            switch (stmt)
            {
                case AddStmt addStmt:
                    t = Types.JavaTypeFor(addStmt.Variable.Type);
                    WriteExpr(addStmt.Variable);
                    Write($".{t.MutatorMethodName}(");
                    WriteExpr(addStmt.Value);
                    WriteLine(");");
                    break;

                case AssertStmt assertStmt:
                    Write("tryAssert(");
                    WriteExpr(assertStmt.Assertion);
                    Write(", ");
                    WriteExpr(assertStmt.Message);
                    WriteLine(");");
                    break;

                case AssignStmt assignStmt:
                    WriteAssignStatement(assignStmt);
                    break;

                case BreakStmt _:
                    WriteLine("break;");
                    break;

                case CompoundStmt compoundStmt:
                    WriteLine("{");
                    foreach (var s in compoundStmt.Statements)
                    {
                        WriteStmt(s);
                    }
                    WriteLine("}");
                    break;

                case ContinueStmt _:
                    WriteLine("continue;");
                    break;

                case CtorStmt _:
                    goto default;

                case FunCallStmt funCallStmt:
                    WriteFunctionCallExpr(funCallStmt.Function, funCallStmt.ArgsList);
                    WriteLine(";");
                    break;

                case GotoStmt gotoStmt:
                    Write($"gotoState({Names.IdentForState(gotoStmt.State)}");
                    if (gotoStmt.Payload != null)
                    {
                        Write(", Optional.of(");
                        WriteExpr(gotoStmt.Payload);
                        Write(")");
                    }
                    WriteLine(");");
                    WriteLine("return;");
                    break;

                case IfStmt ifStmt:
                    Write("if (");
                    WriteExpr(ifStmt.Condition);
                    Write(") ");

                    if (ifStmt.ThenBranch.Statements.Count == 0)
                    {
                        Write("{}");
                    }
                    else
                    {
                        WriteStmt(ifStmt.ThenBranch);
                    }

                    if (ifStmt.ElseBranch != null && ifStmt.ElseBranch.Statements.Count > 0)
                    {
                        WriteLine(" else ");
                        WriteStmt(ifStmt.ElseBranch);
                    }
                    break;


                case InsertStmt insertStmt:
                    t = Types.JavaTypeFor(insertStmt.Variable.Type);
                    WriteExpr(insertStmt.Variable);
                    Write($".{t.MutatorMethodName}(");
                    WriteExpr(insertStmt.Value);
                    WriteLine(");");
                    break;

                case MoveAssignStmt moveAssignStmt:
                    WriteMoveAssignStatement(moveAssignStmt);
                    break;

                case NoStmt _:
                    break;

                case PrintStmt printStmt:
                    Write("System.out.println(");
                    WriteExpr(printStmt.Message);
                    WriteLine(");");
                    break;

                case RaiseStmt raiseStmt:
                    Write("tryRaiseEvent(new ");
                    WriteExpr(raiseStmt.PEvent);
                    Write("(");
                    foreach (var (sep, expr) in raiseStmt.Payload.WithPrefixSep(", "))
                    {
                        Write(sep);
                        WriteExpr(expr);
                    }
                    Write(")");
                    WriteLine(");");
                    WriteLine("return;");
                    break;

                case ReceiveStmt _:
                    goto default;

                case RemoveStmt removeStmt:
                    t = Types.JavaTypeFor(removeStmt.Variable.Type);
                    WriteExpr(removeStmt.Variable);
                    Write($".{t.RemoveMethodName}(");
                    WriteExpr(removeStmt.Value);
                    WriteLine(");");
                    break;

                case ReturnStmt returnStmt:
                    Write("return ");
                    if (returnStmt.ReturnValue != null)
                    {
                        WriteExpr(returnStmt.ReturnValue);
                    }
                    WriteLine(";");
                    break;

                case SendStmt _:
                    goto default;

                case ForeachStmt foreachStmt:
                {
                    string varname = Names.GetNameForDecl(foreachStmt.Item);
                    t = Types.JavaTypeFor(foreachStmt.Item.Type);

                    Write($"for ({t.TypeName} {varname} : ");
                    WriteExpr(foreachStmt.IterCollection);
                    Write(") ");
                    WriteStmt(foreachStmt.Body);
                    break;
                }
                case WhileStmt whileStmt:
                    Write("while (");
                    WriteExpr(whileStmt.Condition);
                    Write(") ");
                    WriteStmt(whileStmt.Body);
                    break;

                case AnnounceStmt _:
                    goto default;

                default:
                    WriteLine($"// TODO: {stmt}");
                    return;
                    //throw new NotImplementedException(stmt.GetType().ToString());
            }
        }

        private void WriteAssignStatement(AssignStmt assignStmt)
        {
            IPExpr lval = assignStmt.Location;
            TypeManager.JType t = Types.JavaTypeForVarLocation(lval);

            IPExpr rval = assignStmt.Value;

            switch (lval)
            {
                case MapAccessExpr mapAccessExpr:
                    WriteExpr(mapAccessExpr.MapExpr);
                    Write($".{t.MutatorMethodName}(");
                    WriteExpr(mapAccessExpr.IndexExpr);
                    Write(",");
                    WriteExpr(rval);
                    WriteLine(");");
                    break;

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    WriteExpr(namedTupleAccessExpr.SubExpr);
                    Write(".");
                    Write(namedTupleAccessExpr.FieldName);
                    Write(" = ");
                    WriteExpr(rval);
                    WriteLine(";");
                    break;

                case SeqAccessExpr seqAccessExpr:
                    // TODO: do we need to think about handling out of bounds exceptions?
                    WriteExpr(seqAccessExpr.SeqExpr);
                    Write($".{t.MutatorMethodName}(");
                    WriteExpr(seqAccessExpr.IndexExpr);
                    Write(",");
                    WriteExpr(rval);
                    WriteLine(");");
                    break;

                case TupleAccessExpr tupleAccessExpr:
                    WriteExpr(tupleAccessExpr.SubExpr);
                    Write($".put({tupleAccessExpr.FieldNo.ToString()}, ");
                    WriteExpr(rval);
                    WriteLine(");");
                    break;

                case VariableAccessExpr variableAccessExpr:
                    Write(Names.GetNameForDecl(variableAccessExpr.Variable));
                    Write(" = ");
                    WriteExpr(rval);
                    WriteLine(";");
                    break;
            }
        }

        private void WriteMoveAssignStatement(MoveAssignStmt moveAssignStmt)
        {
            if (!moveAssignStmt.FromVariable.Type.IsSameTypeAs(moveAssignStmt.ToLocation.Type))
            {
                throw new NotImplementedException("Typecasting in MoveAssignStmt is not yet implemented.");
            }
            IPExpr lval = moveAssignStmt.ToLocation;
            IPExpr rval = new VariableAccessExpr(moveAssignStmt.SourceLocation, moveAssignStmt.FromVariable);

            AssignStmt assignStmt = new AssignStmt(moveAssignStmt.SourceLocation, lval, rval);
            WriteAssignStatement(assignStmt);
        }

        private void WriteFunctionCallExpr(Function f, IEnumerable<IPExpr> args)
        {
            bool isStatic = f.Owner == null;
            if (isStatic && !f.IsForeign)
            {
                throw new NotImplementedException("StaticFunCallExpr is not implemented.");
            }

            string ffiBridge = (f.IsForeign && !isStatic)
                ? Names.FFIBridgeForMachine(_currentMachine.Name) + "."
                : "";
            string fname = Names.GetNameForDecl(f);

            Write($"{ffiBridge}{fname}(");
            foreach (var (sep, expr) in args.WithPrefixSep(", "))
            {
                Write(sep);
                WriteExpr(expr);
            }
            Write($")");
        }

        private void WriteExpr(IPExpr expr)
        {
            TypeManager.JType t;

            switch (expr)
            {
                case BinOpExpr binOpExpr:
                    WriteBinOp(binOpExpr.Lhs, binOpExpr.Operation, binOpExpr.Rhs);
                    break;
                case BoolLiteralExpr ble:
                    Write($"({TypeManager.JType.JBool.ToJavaLiteral(ble.Value)})");
                    break;
                case CastExpr ce:
                {
                    t = Types.JavaTypeFor(ce.Type);
                    //TODO: I am 99% sure it's fine to never worry about casting to the boxed type.
                    Write($"(");
                    Write($"({t.TypeName})");
                    WriteExpr(ce.SubExpr);
                    Write($")");
                    break;
                }
                case ChooseExpr _:
                    goto default; //TODO
                case CloneExpr ce:
                    t = Types.JavaTypeFor(ce.Type);
                    WriteClone(t, () => WriteExpr(ce.Term));
                    break;
                case CoerceExpr _:
                    goto default; //TODO
                case ContainsExpr ce:
                {
                    t = Types.JavaTypeFor(ce.Collection.Type);
                    WriteExpr(ce.Collection);
                    Write($".{t.ContainsMethodName}(");
                    WriteExpr(ce.Item);
                    Write(")");
                    break;
                }
                case CtorExpr _:
                    goto default;
                case DefaultExpr de:
                    t = Types.JavaTypeFor(de.Type);
                    Write(t.DefaultValue);
                    break;
                case EnumElemRefExpr ee:
                    string typeName = ee.Value.ParentEnum.Name;
                    string valueName = ee.Value.Name;
                    Write($"{Constants.TypesNamespaceName}.{typeName}.{valueName}");
                    break;
                case EventRefExpr _:
                    goto default; //TODO
                case FairNondetExpr _:
                    goto default;
                case FloatLiteralExpr fe:
                    Write(TypeManager.JType.JFloat.ToJavaLiteral(fe.Value));
                    break;
                case FunCallExpr fe:
                    WriteFunctionCallExpr(fe.Function, fe.Arguments);
                    break;
                case IntLiteralExpr ie:
                    Write(TypeManager.JType.JInt.ToJavaLiteral(ie.Value));
                    break;
                case KeysExpr ke:
                {
                    t = Types.JavaTypeFor(ke.Expr.Type);
                    if (!(t is TypeManager.JType.JMap mt))
                    {
                        throw new Exception($"Got an unexpected {t.TypeName} rather than a Map");
                    }

                    // Note: P key sets are in fact sequences, so get the j.l.Set() and then
                    // construct a new List from that collection.
                    Write($"new {mt.KeyCollectionType}(");
                    WriteExpr(ke.Expr);
                    Write($".{mt.KeysMethodName}()");
                    Write(")");
                    break;
                }

                case NamedTupleExpr te:
                {
                    t = Types.JavaTypeFor(te.Type);
                    Write($"new {t.TypeName}(");
                    foreach (var (sep, field) in te.TupleFields.WithPrefixSep(", "))
                    {
                        Write(sep);
                        WriteExpr(field);
                    }
                    Write(")");
                    break;
                }
                case NondetExpr _:
                    goto default; // TODO
                case NullLiteralExpr _:
                    Write("null");
                    break;
                case SizeofExpr se:
                    WriteExpr(se.Expr);
                    Write(".size()");
                    break;
                case StringExpr se:
                {
                    string fmtLit = TypeManager.JType.JString.ToJavaLiteral(se.BaseString);
                    if (se.Args.Count == 0)
                    {
                        Write(fmtLit);
                    }
                    else
                    {
                        Write($"java.text.MessageFormat.format({fmtLit}");
                        foreach (var arg in se.Args)
                        {
                            Write(", ");
                            WriteExpr(arg);
                        }

                        Write(")");
                    }

                    break;
                }
                case ThisRefExpr _:
                    goto default;
                case UnaryOpExpr ue:
                    switch (ue.Operation)
                    {
                        case UnaryOpType.Negate:
                            Write("-");
                            break;
                        case UnaryOpType.Not:
                            Write("!");
                            break;

                    }
                    Write("(");
                    WriteExpr(ue.SubExpr);
                    Write(")");
                    break;
                case UnnamedTupleExpr te:
                {
                    t = Types.JavaTypeFor(te.Type);
                    Write($"new {t.TypeName}(");
                    foreach (var (sep, field) in te.TupleFields.WithPrefixSep(", "))
                    {
                        Write(sep);
                        WriteExpr(field);
                    }
                    Write(")");
                    break;
                }
                case ValuesExpr ve:
                {
                    t = Types.JavaTypeFor(ve.Expr.Type);
                    if (!(t is TypeManager.JType.JMap mt))
                    {
                        throw new Exception($"Got an unexpected {t.TypeName} rather than a Map");
                    }

                    // Note: P key sets are in fact sequences, so get the j.l.Set() and then
                    // construct a new List from that collection.
                    Write($"new {mt.ValueCollectionType}(");
                    WriteExpr(ve.Expr);
                    Write($".{mt.KeysMethodName}()");
                    Write(")");
                    break;
                }

                case VariableAccessExpr variableAccessExpr:
                    Write(Names.GetNameForDecl(variableAccessExpr.Variable));
                    break;

                case MapAccessExpr _:
                case NamedTupleAccessExpr _:
                case SetAccessExpr _:
                case SeqAccessExpr _:
                case TupleAccessExpr _:
                    WriteStructureAccess(expr);
                    break;

                default:
                    throw new NotImplementedException(expr.ToString());
            }
        }

        private void WriteBinOp(IPExpr left, BinOpType op, IPExpr right)
        {
            void WritePrim()
            {
                WriteExpr(left); Write($" {op.JavaPrimitiveBinOp()} "); WriteExpr(right);
            }

            TypeManager.JType lhsType = Types.JavaTypeFor(left.Type);
            TypeManager.JType rhsType = Types.JavaTypeFor(right.Type);

            switch (op.GetKind())
            {
                // Numeric and boolean operations are straightforward, since `left` and `right` will either be
                // primitive types or boxed primitive types (in which case we let auto-unboxing do its thing)
                case BinOpKind.Boolean:
                case BinOpKind.Numeric:
                    WritePrim();
                    break;

                // Comparison and equality operators are less straightforward, because we have to emit different
                // Java code depending on whether they are primitive types (i.e. "left == right") versus reference
                // types (i.e. "left.equals(right) == true").

                // So long as both sides are primitive types, this is easy, just like the above case.
                case BinOpKind.Comparison when lhsType.IsPrimitive && rhsType.IsPrimitive:
                case BinOpKind.Equality when lhsType.IsPrimitive && rhsType.IsPrimitive:
                    WritePrim();
                    break;

                // For reference types, defer to Values.compare() or Values.deepEqual() , which
                // will runtime-dispatch on the arguments' classes.
                case BinOpKind.Comparison:
                    Write("(");

                    Write($"{Constants.PrtCompareMethodName}(");
                    WriteExpr(left);
                    Write(", ");
                    WriteExpr(right);
                    Write(")");

                    Write($" {op.JavaPrimitiveBinOp()} 0");
                    Write(")");
                    break;

                case BinOpKind.Equality:
                    Write("(");

                    Write($"{Constants.PrtDeepEqualsMethodName}(");
                    WriteExpr(left);
                    Write(", ");
                    WriteExpr(right);
                    Write(")");

                    Write($" {op.JavaPrimitiveBinOp()} true");
                    Write(")");
                    break;
            }
        }

        private void WriteStructureAccess(IPExpr e)
        {
            TypeManager.JType t = Types.JavaTypeForVarLocation(e);

            // We have to explicitly cast accesses to collections since we might be upcasting (say,
            // if we're extracting an int out of a tuple (List<Object>).).  Use the reference
            // type name to ensure we're casting to another Object (and let Java handle auto-unboxing
            // if it can.)
            //
            // Note: P collections are covariant (i.e. seq[int] extends seq[any]).  This means we'll
            // throw if we downcast and then try to assign a different type into it.  Confirm that
            // this is okay (or at least undefined in the language spec).
            // Write($"({Types.JavaTypeFor(e.Type).ReferenceTypeName})");

            switch (e) {
                case MapAccessExpr mapAccessExpr:
                    WriteExpr(mapAccessExpr.MapExpr);
                    Write($".{t.AccessorMethodName}(");
                    WriteExpr(mapAccessExpr.IndexExpr);
                    Write(")");
                    break;

                case NamedTupleAccessExpr namedTupleAccessExpr:
                    WriteExpr(namedTupleAccessExpr.SubExpr);
                    Write($".{namedTupleAccessExpr.FieldName}");
                    break;

                case SetAccessExpr setAccessExpr:
                    Write($"{Constants.PrtSetElementAtMethodName}(");
                    WriteExpr(setAccessExpr.SetExpr);
                    Write(", ");
                    WriteExpr(setAccessExpr.IndexExpr);
                    Write(")");
                    break;

                case SeqAccessExpr seqAccessExpr:
                    // TODO: do we need to think about handling out of bounds exceptions?
                    WriteExpr(seqAccessExpr.SeqExpr);
                    Write($".{t.AccessorMethodName}(");
                    WriteExpr(seqAccessExpr.IndexExpr);
                    Write(")");
                    break;

                case TupleAccessExpr tupleAccessExpr:
                    WriteExpr(tupleAccessExpr.SubExpr);
                    Write($".{tupleAccessExpr.FieldNo.ToString()}");
                    break;

            }
        }

    }
}
