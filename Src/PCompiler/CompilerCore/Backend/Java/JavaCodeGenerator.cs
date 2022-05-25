using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;

namespace Plang.Compiler.Backend.Java {

    public class JavaCodeGenerator : ICodeGenerator
    {

        private CompilationContext context;
        private CompiledFile source;
        private Scope globalScope;
        
        /// <summary>
        /// Generates Java code for a given compilation job.
        ///
        /// Currently, we should be able to use nested classes to put everything we need in a single
        /// Java file, in a manner similar to how the C# extractor uses namesspaces.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="globalScope"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope scope)
        {
            context = new CompilationContext(job);
            source = new CompiledFile(context.FileName);
            globalScope = scope;

            WriteImports();
            WriteLine();
           
            WriteLine($"static class {SourceTemplates.TopLevelClassName} {{");

            foreach (var e in globalScope.Events)
            {
                WriteEventDecl(e);     
            }
            WriteLine();
            
            //TODO: Do specs need interfaces?

            foreach (var m in globalScope.Machines)
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
            
            return new List<CompiledFile> { source };
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
            string eventName = context.Names.GetNameForDecl(e);
            
            // XXX: If e.PayloadType is PrimitiveType.Null, this produces an 
            // extraneous value.
            TypeManager.JType argType = context.Types.JavaTypeFor(e.PayloadType, true);
            
            WriteLine($"record {eventName}({argType.TypeName} payload) implements Event.Payload {{ }} ");
        }

        private void WriteMachineDecl(Machine m)
        {
            WriteLine($"// PMachine {m.Name} elided ");
        }
        
        private void WriteMonitorDecl(Machine m)
        {
            WriteLine($"static class {m.Name} extends Monitor {{");

            foreach (Variable field in m.Fields)
            {
                TypeManager.JType type = context.Types.JavaTypeFor(field.Type);
                string name = context.Names.GetNameForDecl(field);
                
                WriteLine($"private {type.TypeName} {name} = {type.DefaultValue}; //TODO");
            }
            
            
            WriteLine($"}} // {m.Name} monitor definition");
        }

        private void WriteLine(string s = "")
        {
            context.WriteLine(source.Stream, s);
        }

        private void Write(string s)
        {
            context.Write(source.Stream, s);
        }
    }
}
