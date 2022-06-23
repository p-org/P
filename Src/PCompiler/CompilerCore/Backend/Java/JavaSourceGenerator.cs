using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler.Backend.Java
{
    public abstract class JavaSourceGenerator : ICodeGenerator
    {
        private CompilationContext _context;
        protected CompiledFile Source;
        protected Scope GlobalScope;

        internal NameManager Names => _context.Names;
        internal TypeManager Types => _context.Types;

        /// <summary>
        /// Constructs a new Java source generator for a particular output file.
        /// </summary>
        /// <param name="filename"></param>
        internal JavaSourceGenerator(string filename)
        {
            Source = new CompiledFile(filename);
        }

        private void Initialize(ICompilationJob job, Scope scope)
        {
            _context = new CompilationContext(job);
            GlobalScope = scope;
        }

        private void WriteFileHeader()
        {
            WriteLine("package PGenerated;");

            WriteLine(Constants.DoNotEditWarning);
            WriteLine();

            WriteImports();
            WriteLine();
        }

        private void WriteImports()
        {
            foreach (var stmt in Constants.ImportStatements())
            {
                WriteLine(stmt);
            }
        }

        /// <summary>
        /// Executes the compilation stage for the derived Java source generator.
        /// After initialising the output file and writing standard boilerplate like
        /// imports and "do not edit" comment blocks, the functionality in `GenerateCodeImpl`
        /// will be invoked.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="scope"></param>
        /// <returns>The single compiled file.</returns>
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope scope)
        {
            Initialize(job, scope);
            WriteFileHeader();
            GenerateCodeImpl();
            return new List<CompiledFile> { Source };
        }

        /// <summary>
        /// Generates the code for this generator's particular file into `this.Source`.
        /// Each subclass must implement this functionality.
        /// </summary>
        protected abstract void GenerateCodeImpl();


        internal void WriteClone(TypeManager.JType t, Action writeTermToBeCloned)
        {
            // Note: We elide calls to Clone for types that are either immutable
            // or can unbox to copy-by-value types.  To despecialise these cases,
            // fall through to the non-boxable reference type case.
            switch (t)
            {
                /* Primitive types are easy since they're copy by value. */
                case TypeManager.JType.JVoid _:
                case TypeManager.JType.JBool _:
                case TypeManager.JType.JInt _:
                case TypeManager.JType.JFloat _:
                    writeTermToBeCloned();
                    break;

                /* Same with immutable types. */
                case TypeManager.JType.JString _:
                case TypeManager.JType.JMachine _:
                    writeTermToBeCloned();
                    break;

                /* Non-boxable reference types must be cloned explicitly and then
                 * cast to their expected type (since clone() is Object-producing). */
                case TypeManager.JType.JAny _:
                case TypeManager.JType.JMap _:
                case TypeManager.JType.JList _:
                case TypeManager.JType.JSet _:
                case TypeManager.JType.JForeign _: //TODO: is this right?
                    Write($"({t.TypeName})");
                    Write($"{Constants.PrtDeepCloneMethodName}(");
                    writeTermToBeCloned();
                    Write(")");
                    break;

                /* JNamedTuples have a copy constructor. */
                case TypeManager.JType.JNamedTuple _:
                    writeTermToBeCloned();
                    Write(".deepClone()");
                    break;

                default:
                    throw new NotImplementedException(t.ToString());
            }
        }
        protected void WriteLine(string s = "")
        {
            _context.WriteLine(Source.Stream, s);
        }

        protected void Write(string s)
        {
            _context.Write(Source.Stream, s);
        }
    }
}
