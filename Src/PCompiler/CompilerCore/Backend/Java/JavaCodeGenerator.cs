using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.States;

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
           
            WriteLine($"static class {SourceTemplates.TopLevelClassName} {{");

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
            
            WriteLine($"}} // {SourceTemplates.TopLevelClassName} class definition");
            
            return new List<CompiledFile> { _source };
        }

        private void WriteImports()
        {
            foreach (var className in SourceTemplates.ImportStatements())
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
            
            // Event handlers
            foreach (var s in m.States)
            {
                foreach (var (e, a) in s.AllEventHandlers)
                {
                    WriteEventHandler(e, a);
                }
            }
            // constructor
            WriteMonitorCstr(m);
            

            WriteLine($"}} // {cname} monitor definition");
        }

        private void WriteEventHandler(PEvent e, IStateAction a)
        {
            switch (a)
            {
                case EventDefer d:
                    break;
                case EventDoAction da:
                    break;
                case EventGotoState gs:
                    break;
                case EventIgnore i:
                    break;
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
            WriteLine($".isInitialState({s.IsStart.ToString()})");
            WriteLine(".build());");
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
