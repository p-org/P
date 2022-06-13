using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Antlr4.Runtime;
using Plang.Compiler.Backend.ASTExt;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.AST.Statements;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java {

    public class JavaCodeGenerator : ICodeGenerator
    {

        private CompilationContext _context;
        private CompiledFile _source;
        private Scope _globalScope;

        // In the C# and Rvm compilers, event handlers' signatures are hard-coded to
        // be an abstract Event type.  However, our Java runtime takes advantage of the
        // typechecker to be able to specify exactly _which_ event it is going to be
        // handed.  This isn't something explicitly handed to us in the AST so we
        // accumulate it ourselves before proceeding with code generation.
        private Dictionary<Function, PEvent> _eventArgForFn;

        /// <summary>
        /// Generates Java code for a given compilation job.
        ///
        /// Currently, we should be able to use nested classes to put everything we need in a single
        /// Java file, in a manner similar to how the C# extractor uses namespaces.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope scope)
        {
            _context = new CompilationContext(job);
            _source = new CompiledFile(_context.FileName);
            _globalScope = scope;

            _eventArgForFn = GenerateFunctionToArgMapping();

            WriteImports();
            WriteLine();

            WriteLine(Constants.DoNotEditWarning);
            WriteLine();


            WriteLine($"public class {_context.FileName.Replace(".java", "")} {{");

            WriteLine("/** Enums */");
            foreach (var e in _globalScope.Enums)
            {
                WriteEnumDecl(e);
            }
            WriteLine();

            WriteLine("/** Tuples */");
            foreach (var t in _globalScope.Tuples)
            {
                WriteNamedTupleDecl(t);
            }
            WriteLine();

            WriteLine("/** Events */");
            foreach (var e in _globalScope.Events)
            {
                WriteEventDecl(e);
            }
            WriteLine();

            //TODO: Do specs need interfaces?

            foreach (var m in _globalScope.Machines)
            {
                if (m.IsSpec)
                {
                    WriteMonitorDecl(m);
                }
                else
                {
                    WriteMachineDecl(m);
                }
            }

            WriteLine($"}} // {_context.FileName} class definition");

            return new List<CompiledFile> { _source };
        }

        private Dictionary<Function, PEvent> GenerateFunctionToArgMapping()
        {
            Dictionary<Function, PEvent> ret = new Dictionary<Function, PEvent>();


            foreach (var m in _globalScope.Machines)
            {
                foreach (var s in m.States)
                {
                    foreach (var (e, a) in s.AllEventHandlers)
                    {
                        Function f;
                        switch (a)
                        {
                            case EventDoAction da:
                                f = da.Target;
                                break;
                            case EventGotoState { TransitionFunction: { } } gs:
                                f = gs.TransitionFunction;
                                break;
                            case EventDefer _:
                                continue;
                            case EventIgnore _:
                                continue;
                            default:
                                continue;
                        }

                        PEvent e2;
                        if (ret.TryGetValue(f, out e2))
                        {
                            if (f.Signature.Parameters.Count > 0 && e != e2)
                            {
                                string name = (f.IsAnon ? $"function at line {f.SourceLocation.Start.Line}" : f.Name);
                                throw new Exception(
                                    $"Inconsistent argument type to {name}: seen both {e2.Name} and {e.Name}");
                            }
                        }
                        else
                        {
                            ret.Add(f, e);
                        }
                    }
                }
            }

            return ret;
        }

        private void WriteImports()
        {
            foreach (var stmt in Constants.ImportStatements())
            {
                WriteLine(stmt);
            }
        }

        private void WriteNamedTupleDecl(NamedTupleType t)
        {
            // This is a sequence of <type, stringName> pairs.
            List<(TypeManager.JType, string)> fields =
                new List<(TypeManager.JType, string)>();

            // Build up our list of fields.
            foreach (var e in t.Fields)
            {
                string name = e.Name;
                PLanguageType type = e.Type;

                // In the case where the field type is a typedef, follow
                // the typename resolution until we've found the actual type.
                while (type is TypeDefType tdef)
                {
                    type = tdef.TypeDefDecl.Type;
                }

                TypeManager.JType jType = _context.Types.JavaTypeFor(type);

                fields.Add((jType, name));
            }

            string tname = _context.Names.NameForNamedTuple(t);
            WriteLine($"// {t.CanonicalRepresentation}");
            WriteLine($"static class {tname} implements Values.PTuple<{tname}> {{");

            // Write the fields.
            foreach (var (jType, fieldName) in fields)
            {
                WriteLine($"public {jType.TypeName} {fieldName};");
            }
            WriteLine();

            // Write the default constructor.
            WriteLine($"public {tname}() {{");
            foreach (var (jtype, fieldName) in fields)
            {
                WriteLine($"this.{fieldName} = {jtype.DefaultValue};");

            }
            WriteLine($"}}");
            WriteLine();

            // Write the explicit constructor.
            Write($"public {tname}(");
            foreach (var ((jType, fieldName), sep) in fields.Select((pair, i) => (pair, i > 0 ? ", " : "")))
            {
                Write($"{sep}{jType.TypeName} {fieldName}");
            }
            WriteLine($") {{");
            foreach (var (jtype, fieldName) in fields)
            {
                WriteLine($"this.{fieldName} = {fieldName};");
            }
            WriteLine($"}}");
            WriteLine();

            // Write the copy constructor for cloning.
            WriteLine($"public {tname} deepClone() {{");
            Write($"return new {tname}(");
            foreach (var ((jType, fieldName), sep) in fields.Select((pair, i) => (pair, i > 0 ? ", " : "")))
            {
                Write(sep);

                /* Note: this looks _a lot_ like CloneExpr().  Can we make this more harmonious? */
                switch (jType)
                {
                /* Primitive types are easy since they're copy by value. */
                case TypeManager.JType.JVoid _:
                case TypeManager.JType.JBool _:
                case TypeManager.JType.JInt _:
                case TypeManager.JType.JFloat _:
                case TypeManager.JType.JString _:
                case TypeManager.JType.JMachine _:
                    Write($"{fieldName}");
                    break;

                /* Non-boxable reference types must be cloned explicitly and then
                 * cast to their expected type (since clone() is Object-producing). */
                case TypeManager.JType.JMap _:
                case TypeManager.JType.JList _:
                case TypeManager.JType.JSet _:
                    Write($"({jType.TypeName})Values.deepClone({fieldName})");
                    break;

                /* JNamedTuples have a copy constructor which avoids us having
                 * to bounce through Values.deepClone(). */
                case TypeManager.JType.JNamedTuple nt:
                    Write($"{fieldName}.deepClone()");
                    break;

                default:
                    throw new NotImplementedException(jType.ToString());
                }
            }
            WriteLine(");");
            WriteLine("} // deepClone()");
            WriteLine();

            // Deep equality predicate.
            WriteLine($"public boolean deepEquals({tname} o2) {{");
            WriteLine("return Values.deepEquals(this, o2);");
            WriteLine("} // deepEquals()");
            WriteLine();

            // Write toString() in the same output style as a Java record.
            WriteLine("public String toString() {");
            WriteLine($"StringBuilder sb = new StringBuilder(\"{tname}\");");
            WriteLine("sb.append(\"[\");");
            foreach (var ((_, fieldName), sep) in fields.Select((pair, i) => (pair, i > 0 ? "," : "")))
            {
                WriteLine($"sb.append(\"{sep}{fieldName}=\" + {fieldName});");
            }
            WriteLine("sb.append(\"]\");");
            WriteLine("return sb.toString();");
            WriteLine("} // toString()");

            WriteLine($"}} //{tname} class definition");

            WriteLine();
        }

        private void WriteEnumDecl(PEnum e)
        {
            WriteLine($"public static class {e.Name} {{");

            foreach (var param in e.Values)
            {
                WriteLine($"public static final int {param.Name} = {param.Value};");
            }

            WriteLine("}");
        }

        private void WriteEventDecl(PEvent e)
        {
            string eventName = _context.Names.GetNameForDecl(e);

            TypeManager.JType argType = _context.Types.JavaTypeFor(e.PayloadType);
            switch (argType)
            {
                case TypeManager.JType.JVoid _:
                    // Special-case an event with no payload: just emit an empty record.
                    WriteLine($"record {eventName}() implements PObserveEvent.PEvent {{ }} ");
                    break;
                default:
                    WriteLine($"record {eventName}({argType.TypeName} payload) implements PObserveEvent.PEvent {{ }} ");
                    break;
            }

        }

        private void WriteMachineDecl(Machine m)
        {
            WriteLine($"// PMachine {m.Name} elided ");
        }

        private void WriteMonitorDecl(Machine m)
        {
            string cname = _context.Names.GetNameForDecl(m);

            WriteLine($"static class {cname} extends Monitor {{");

            // monitor fields
            foreach (var field in m.Fields)
            {
                TypeManager.JType type = _context.Types.JavaTypeFor(field.Type);
                string name = _context.Names.GetNameForDecl(field);
                string methodName = name[0].ToString().ToUpper() + name.Substring(1);

                WriteLine($"private {type.TypeName} {name} = {type.DefaultValue};");
                WriteLine($"public {type.TypeName} get{methodName}() {{ return this.{name}; }};");
                WriteLine();
            }
            WriteLine();

            // state identifiers
            foreach (var s in m.States)
            {
                //TODO: I think it's fine to use unqualified names here.  But, confirm.
                WriteLine($"public String {_context.Names.IdentForState(s)} = \"{s.Name}\";");
            }
            WriteLine();

            // functions
            foreach (var f in m.Methods)
            {
                WriteFunction(f);
            }
            WriteLine();

            // constructor
            WriteMonitorCstr(m);


            WriteLine($"}} // {cname} monitor definition");
        }


        private void WriteFunction(Function f)
        {
            if (f.IsForeign)
            {
                WriteLine($"// Foreign function {f.Name} elided");
            }

            if (f.CanReceive == true)
            {
                WriteLine($"// Async function {f.Name} elided");
            }

            WriteFunctionSignature(f); WriteLine(" {");

            if (f.IsAnon && f.Signature.Parameters.Any())
            {
                Variable p = f.Signature.Parameters.First();
                TypeManager.JType t = _context.Types.JavaTypeFor(p.Type);
                string name = _context.Names.GetNameForDecl(p);

                WriteLine($"{t.TypeName} {name} = pEvent.payload;");
            }

            foreach (var decl in f.LocalVariables)
            {
                //TODO: for reference types the default value can simply be null; it will be reassigned later.
                TypeManager.JType t = _context.Types.JavaTypeFor(decl.Type);
                WriteLine($"{t.TypeName} {_context.Names.GetNameForDecl(decl)} = {t.DefaultValue};");
            }
            WriteLine();

            foreach (var stmt in f.Body.Statements)
            {
                WriteStmt(stmt);
            }

            WriteLine("}");
        }

        private void WriteFunctionSignature(Function f)
        {
            string fname = _context.Names.GetNameForDecl(f);

            Write("private ");

            bool isStatic = f.Owner == null;
            if (isStatic)
            {
                Write("static ");
            }


            string args;
            if (f.IsAnon)
            {
                args = $"{_eventArgForFn[f].Name} pEvent";
            }
            else
            {
                args = string.Join(
                    ",",
                    f.Signature.Parameters.Select(v =>
                        $"{_context.Types.JavaTypeFor(v.Type).TypeName} {_context.Names.GetNameForDecl(v)}"));
            }

            TypeManager.JType retType = _context.Types.JavaTypeFor(f.Signature.ReturnType);

            Write($"{retType.TypeName} {fname}({args})");

            // If this function has exceptional control flow (for raising events or state transition)
            // we need to annotate it appropriately.
            List<string> throwables = new List<string>();
            if (f.CanChangeState == true)
            {
                throwables.Add("TransitionException");
            }
            if (f.CanRaiseEvent == true)
            {
                throwables.Add("RaiseEventException");
            }
            if (throwables.Count > 0)
            {
                Write($"throws {string.Join(", ", throwables)}");
            }

        }

        private void WriteMonitorCstr(Machine m)
        {
            string cname = _context.Names.GetNameForDecl(m);

            WriteLine($"public {cname}() {{");
            WriteLine("super();");

            foreach (var s in m.States)
            {
                WriteStateBuilderDecl(s);
            }
            WriteLine("} // constructor");
        }

        private void WriteStateBuilderDecl(State s)
        {
            WriteLine($"addState(new State.Builder({_context.Names.IdentForState(s)})");
            WriteLine($".isInitialState({TypeManager.JType.JBool.ToJavaLiteral(s.IsStart)})");

            foreach (var (e, a) in s.AllEventHandlers)
            {
                WriteStateBuilderEventHandler(e, a);
            }

            WriteLine(".build());");
        }

        private void WriteStateBuilderEventHandler(PEvent e, IStateAction a)
        {
            string ename = _context.Names.GetNameForDecl(e);

            switch (a)
            {
                case EventDefer _:
                    WriteLine($"// Ignoring deferred event {ename}");
                    break;
                case EventDoAction da:
                {
                    string aname = _context.Names.GetNameForDecl(da.Target);
                    WriteLine($".withEvent({ename}.class, this::{aname})");
                    break;
                }
                case EventGotoState { TransitionFunction: null } gs:
                {
                    string sname = _context.Names.IdentForState(gs.Target);
                    WriteLine($".withEvent({ename}.class, __ -> gotoState({sname}))");
                    break;
                }
                case EventGotoState { TransitionFunction: { } } gs:
                {
                    string sname = _context.Names.IdentForState(gs.Target);
                    string tname = _context.Names.GetNameForDecl(gs.TransitionFunction);
                    WriteLine($".withEvent({ename}.class, e -> {{ {tname}(e); gotoState({sname}); }})");
                    break;
                }
                case EventIgnore _:
                    WriteLine($".withEvent({ename}.class, __ -> {{ ; }})");
                    break;
                default:
                    throw new NotImplementedException($"TODO: {a.GetType()} not implemented.");
            }
        }

        private void WriteStmt(IPStmt stmt)
        {
            TypeManager.JType t;

            switch (stmt)
            {
                case AddStmt addStmt:
                    t = _context.Types.JavaTypeFor(addStmt.Variable.Type);
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
                    WriteFunctionCall(funCallStmt.Function, funCallStmt.ArgsList);
                    break;

                case GotoStmt gotoStmt:
                    Write($"gotoState({_context.Names.IdentForState(gotoStmt.State)}");
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
                    t = _context.Types.JavaTypeFor(insertStmt.Variable.Type);
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
                    foreach (var (param, sep)in raiseStmt.Payload.Select((p, i) => (p, i > 0 ? ", " : "")))
                    {
                        Write(sep);
                        WriteExpr(param);
                    }
                    Write(")");
                    WriteLine(");");
                    WriteLine("return;");
                    break;

                case ReceiveStmt _:
                    goto default;

                case RemoveStmt removeStmt:
                    t = _context.Types.JavaTypeFor(removeStmt.Variable.Type);
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
                    string varname = _context.Names.GetNameForDecl(foreachStmt.Item);
                    t = _context.Types.JavaTypeFor(foreachStmt.Item.Type);

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
            TypeManager.JType t = _context.Types.JavaTypeForVarLocation(lval);

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
                    Write(_context.Names.GetNameForDecl(variableAccessExpr.Variable));
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

        private void WriteFunctionCall(Function f, IEnumerable<IPExpr> args)
        {
            if (f.Owner == null)
            {
                throw new NotImplementedException("StaticFunCallExpr is not implemented.");
            }

            string fname = _context.Names.GetNameForDecl(f);

            Write($"{fname}(");
            foreach (var (param, sep)in args.Select((p, i) => (p, i > 0 ? ", " : "")))
            {
                Write(sep);
                WriteExpr(param);
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
                    t = _context.Types.JavaTypeFor(ce.Type);
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
                    WriteClone(ce);
                    break;
                case CoerceExpr _:
                    goto default; //TODO
                case ContainsExpr ce:
                {
                    t = _context.Types.JavaTypeFor(ce.Collection.Type);
                    WriteExpr(ce.Collection);
                    Write($".{t.ContainsMethodName}(");
                    WriteExpr(ce.Item);
                    Write(")");
                    break;
                }
                case CtorExpr _:
                    goto default;
                case DefaultExpr de:
                    t = _context.Types.JavaTypeFor(de.Type);
                    Write(t.DefaultValue);
                    break;
                case EnumElemRefExpr ee:
                    string typeName = ee.Value.ParentEnum.Name;
                    string valueName = ee.Value.Name;
                    Write($"{typeName}.{valueName}");
                    break;
                case EventRefExpr _:
                    goto default; //TODO
                case FairNondetExpr _:
                    goto default;
                case FloatLiteralExpr fe:
                    Write(TypeManager.JType.JFloat.ToJavaLiteral(fe.Value));
                    break;
                case FunCallExpr fe:
                    WriteFunctionCall(fe.Function, fe.Arguments);
                    break;
                case IntLiteralExpr ie:
                    Write(TypeManager.JType.JInt.ToJavaLiteral(ie.Value));
                    break;
                case KeysExpr ke:
                {
                    t = _context.Types.JavaTypeFor(ke.Expr.Type);
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
                    t = _context.Types.JavaTypeFor(te.Type);
                    Write($"new {t.TypeName}(");
                    foreach (var (field, sep) in te.TupleFields.Select((e, i) => (e, i > 0 ? ", " : "")))
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
                        Write($"MessageFormat.format({fmtLit}");
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
                    t = _context.Types.JavaTypeFor(te.Type);
                    Write($"new {t.TypeName}(");
                    foreach (var (field, sep) in te.TupleFields.Select((e, i) =>
                                 (e, i > 0 ? ", " : "")))
                    {
                        Write(sep);
                        WriteExpr(field);
                    }
                    Write(")");
                    break;
                }
                case ValuesExpr ve:
                {
                    t = _context.Types.JavaTypeFor(ve.Expr.Type);
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
                    Write(_context.Names.GetNameForDecl(variableAccessExpr.Variable));
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

            // This emits a raw comparison operation between `left` and `right`.
            void WriteDirectComparisonExpr(string op)
            {
                Write("(");
                WriteExpr(left);
                Write($" {op} ");
                WriteExpr(right);
                Write(")");
            }

            // This emits a call to the P runtime's `Values.compare()` static method,
            // which returns a value on -1, 0, 1 in the usual Java manner.
            void WriteComparatorCall(string op)
            {
                Write("(");

                Write("Values.compare(");
                WriteExpr(left);
                Write(", ");
                WriteExpr(right);
                Write(")");

                Write($" {op} 0");
                Write(")");
            }

            // This emits a call to the P runtime's `Values.equality()` static method,
            // and compares the result with `op` against the value `true`.
            void WriteEqualitycall(string op)
            {
                Write("(");

                Write("Values.deepEqual(");
                WriteExpr(left);
                Write(", ");
                WriteExpr(right);
                Write(")");

                Write($" {op} true");
                Write(")");
            }

            // Numeric operations are straightforward, since `left` and `right` will either be
            // primitive types or boxed primitive types (in which case we let auto-unboxing do its thing)
            switch (op)
            {
                // Arithmetic operators
                case BinOpType.Add:
                    WriteDirectComparisonExpr("+");
                    return;
                case BinOpType.Sub:
                    WriteDirectComparisonExpr("-");
                    return;
                case BinOpType.Mul:
                    WriteDirectComparisonExpr("*");
                    return;
                case BinOpType.Div:
                    WriteDirectComparisonExpr("/");
                    return;
                case BinOpType.Mod:
                    WriteDirectComparisonExpr("%");
                    return;
            }

            // Note: the following tries to be smart about using auto-unboxing when the left and right-hand
            // sides of the operator can be coersed into a primitive type (and thus `==`, `<`, etc can be
            // used without a method call.)  To disable this for debugging purposes, uncomment the following
            // two lines and all comparisons will go through the Values interface.  Examples of the difference:
            //
            //    TMP_tmp22 = (Values.deepEqual(TMP_tmp21, tTransStatus.ERROR) == true);
            //    TMP_tmp22 = (TMP_tmp21 == tTransStatus.ERROR);
            //
            //    TMP_tmp26 = (Values.compare(TMP_tmp25, 0) > 0);
            //    TMP_tmp26 = (TMP_tmp25 > 0);
            //WriteComparisonBinOp(left, op, right, writeComparatorCall, writeEqualitycall);
            //return;

            // Non-numeric binary operators are comparison operators.  Depending on the
            // type we may be able to use straightforward comparison like "<", but in
            // other cases we will have to fall back on comparators.

            TypeManager.JType lhsType = _context.Types.JavaTypeFor(left.Type);
            TypeManager.JType rhsType = _context.Types.JavaTypeFor(right.Type);
            switch (lhsType, rhsType)
            {
                // Types for which we can use direct operators like "<", "==",
                // emitted via `writeDirectComparisonExpr`.
                case (TypeManager.JType.JBool _, TypeManager.JType.JBool _):
                case (TypeManager.JType.JInt _, TypeManager.JType.JInt _):
                case (TypeManager.JType.JFloat _, TypeManager.JType.JFloat _):
                case (TypeManager.JType.JMachine _, TypeManager.JType.JMachine _):
                    WriteComparisonBinOp(op, WriteDirectComparisonExpr, WriteDirectComparisonExpr);
                    return;

                // Types for which we need non-Java operators (i.e. "Values.deepEquals()", "Values.Compare()", ...)
                // which are emitted via the `writeComparatorCall` delegate.
                default:
                    WriteComparisonBinOp(op, WriteComparatorCall, WriteEqualitycall);
                    return;
            }
        }

        /*
         * compareIt is a delegate that takes an ordering operator and emits a comparison check.
         * equalIt is a delegate that takes an ordering operator and emits an equality check.
         */
        private void WriteComparisonBinOp(BinOpType op, Action<string> compareIt, Action<string> equalIt)
        {
            switch (op)
            {
                // Comparison operators
                case BinOpType.Lt:
                    compareIt("<");
                    break;
                case BinOpType.Le:
                    compareIt("<=");
                    break;
                case BinOpType.Ge:
                    compareIt(">=");
                    break;
                case BinOpType.Gt:
                    compareIt(">");
                    break;

                // Equality operators
                case BinOpType.Neq:
                    equalIt("!=");
                    break;
                case BinOpType.Eq:
                    equalIt("==");
                    break;

                // Arithmetic operators
                case BinOpType.Add:
                case BinOpType.Sub:
                case BinOpType.Mul:
                case BinOpType.Div:
                case BinOpType.Mod:
                default:
                    throw new NotImplementedException(op.ToString());
            }
        }


        private void WriteClone(CloneExpr ce)
        {
            TypeManager.JType t = _context.Types.JavaTypeFor(ce.Term.Type);

            // Note: We elide calls to Clone for types that are either immutable
            // or can unbox to copy-by-value types.  If there's an issue, comment
            // out the first two writExpr; break; s and fall through to the non-boxable
            // reference type case.
            switch (t)
            {
                /* Primitive types are easy since they're copy by value. */
                case TypeManager.JType.JVoid _:
                case TypeManager.JType.JBool _:
                case TypeManager.JType.JInt _:
                case TypeManager.JType.JFloat _:
                    WriteExpr(ce.Term);
                    break;

                /* Same with immutable types. */
                case TypeManager.JType.JString _:
                case TypeManager.JType.JMachine _:
                    WriteExpr(ce.Term);
                    break;

                /* Non-boxable reference types must be cloned explicitly and then
                 * cast to their expected type (since clone() is Object-producing). */
                case TypeManager.JType.JMap _:
                case TypeManager.JType.JList _:
                case TypeManager.JType.JSet _:
                    Write($"({t.TypeName})");
                    Write("Values.deepClone(");
                    WriteExpr(ce.Term);
                    Write(")");
                    break;

                /* JNamedTuples have a copy constructor. */
                case TypeManager.JType.JNamedTuple nt:
                    WriteExpr(ce.Term);
                    Write(".deepClone()");
                    break;

                default:
                    throw new NotImplementedException(t.TypeName);
            }
        }

        private void WriteStructureAccess(IPExpr e)
        {
            TypeManager.JType t = _context.Types.JavaTypeForVarLocation(e);

            // We have to explicitly cast accesses to collections since we might be upcasting (say,
            // if we're extracting an int out of a tuple (List<Object>).).  Use the reference
            // type name to ensure we're casting to another Object (and let Java handle auto-unboxing
            // if it can.)
            //
            // Note: P collections are covariant (i.e. seq[int] extends seq[any]).  This means we'll
            // throw if we downcast and then try to assign a different type into it.  Confirm that
            // this is okay (or at least undefined in the language spec).
            // Write($"({_context.Types.JavaTypeFor(e.Type).ReferenceTypeName})");

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
                    Write("Values.setElementAt(");
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

        private void WriteLine(string s = "")
        {
            _context.WriteLine(_source.Stream, s);
        }

        private void Write(string s)
        {
            _context.Write(_source.Stream, s);
        }
    }
}
