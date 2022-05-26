using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;
using System.Linq;
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

            WriteImports();
            WriteLine();
           
            WriteLine($"static class {Constants.TopLevelClassName} {{");

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
            
            WriteLine($"}} // {Constants.TopLevelClassName} class definition");
            
            return new List<CompiledFile> { _source };
        }

        private void WriteImports()
        {
            foreach (var className in Constants.ImportStatements())
            {
                WriteLine("import " + className);
            }
        }

        private void WriteEventDecl(PEvent e)
        {
            string eventName = _context.Names.GetNameForDecl(e);
            
            // XXX: If e.PayloadType is PrimitiveType.Null, this produces an 
            // extraneous value.
            TypeManager.JType argType = _context.Types.JavaTypeFor(e.PayloadType);
            
            WriteLine($"record {eventName}({argType.TypeName} payload) implements Event.Payload {{ }} ");
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
                
                WriteLine($"private {type.TypeName} {name} = {type.DefaultValue}; //TODO");
            }
            WriteLine();

            // state identifiers
            foreach (var s in m.States)
            {
                //TODO: I think it's fine to use unqualified names here.  But, confirm.
                WriteLine($"private String {_context.Names.IdentForState(s)} = \"{s.Name}\";");
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
            
            TypeManager.JType retType = _context.Types.JavaTypeFor(f.Signature.ReturnType);

            string args = string.Join(
                ",",
                f.Signature.Parameters.Select(v =>
                    $"{_context.Types.JavaTypeFor(v.Type).TypeName} {v.Name}"));
            
            Write($"{retType.TypeName} {fname}({args})");
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
            WriteLine($".isInitialState({s.IsStart.ToString()})");

            foreach (var (e, a) in s.AllEventHandlers)
            {
                WriteStateBuilderEventHandler(e, a);
            }
            
            WriteLine(".build());");
        }
        
        private void WriteStateBuilderEventHandler(PEvent e, IStateAction a)
        {
            string ename = _context.Names.GetNameForDecl(e);
            string aname;
            
            switch (a)
            {
                case EventDoAction da:
                    aname = _context.Names.GetNameForDecl(da.Target);
                    WriteLine($".WithEvent({ename}.class, {aname})");
                    break;
                case EventGotoState gs:
                case EventIgnore i:
                default:
                    throw new NotImplementedException($"TODO: {a.GetType()} not implemented.");
            }
        }

        private void WriteStmt(IPStmt stmt)
        {
            switch (stmt)
            {
                case AssertStmt assertStmt:
                    Write("TryAssert(");
                    WriteExpr(assertStmt.Assertion);
                    Write(", ");
                    WriteExpr(assertStmt.Message);
                    WriteLine(");");
                    break;
                case AssignStmt assignStmt:
                case CompoundStmt compoundStmt:
                case CtorStmt ctorStmt:
                case FunCallStmt funCallStmt:
                case GotoStmt gotoStmt:
                case IfStmt ifStmt:
                case AddStmt addStmt:
                case InsertStmt insertStmt:
                case MoveAssignStmt moveAssignStmt:
                case NoStmt _:
                case PrintStmt printStmt:
                case RaiseStmt raiseStmt:
                case ReceiveStmt receiveStmt:
                case RemoveStmt removeStmt:
                case ReturnStmt returnStmt:
                case BreakStmt breakStmt:
                case ContinueStmt continueStmt:
                case SendStmt sendStmt:
                case ForeachStmt foreachStmt:
                case WhileStmt whileStmt:
                case AnnounceStmt announceStmt:
                default:
                    WriteLine($"// TODO: {stmt}");
                    return;
                    //throw new NotImplementedException(stmt.GetType().ToString());
            }
        }

        private void WriteExpr(IPExpr expr)
        {
            switch (expr)
            {
                case BinOpExpr boe:
                    break;
                case BoolLiteralExpr ble:
                    break;
                case CastExpr ce:
                    break;
                case ChooseExpr ce:
                    break; 
                case CloneExpr ce:
                    break;
                case CoerceExpr ce:
                    break;
                case ContainsExpr ce:
                    break;
                case CtorExpr ce:
                    break;
                case DefaultExpr de:
                    break;
                case EnumElemRefExpr ee:
                    break;
                case EventRefExpr ee:
                    break;
                case FairNondetExpr fe:
                    break;
                case FloatLiteralExpr fe:
                    break;
                case FunCallExpr fe:
                    break;
                case IntLiteralExpr ie:
                    break;
                case KeysExpr ke:
                    break;
                case NamedTupleExpr ne:
                    break;
                case NondetExpr ne:
                    break;
                case NullLiteralExpr ne:
                    break;
                case SizeofExpr se:
                    break;
                case StringExpr se:
                    break;
                case ThisRefExpr te:
                    break;
                case UnaryOpExpr ue:
                    break;
                case UnnamedTupleExpr ue:
                    break;
                case ValuesExpr ve:
                    break;
                
                case MapAccessExpr _:
                case NamedTupleAccessExpr _:
                case SeqAccessExpr _:
                case TupleAccessExpr _:
                case VariableAccessExpr _:
                    WriteStructureAccess(expr);
                    break;
                
                default:
                    throw new NotImplementedException(expr.GetType().ToString());
            }
        }
        
        private void WriteStructureAccess(IPExpr e)
        {
            switch (e)
            {
                case MapAccessExpr mpe:
                    break;
                case SetAccessExpr sae:
                    break;
                case NamedTupleAccessExpr ntae:
                    break;
                case SeqAccessExpr sae:
                    break;
                case TupleAccessExpr tae:
                    break;
                case VariableAccessExpr vae:
                    break;
                
                default:
                    throw new NotImplementedException(e.GetType().ToString());
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
