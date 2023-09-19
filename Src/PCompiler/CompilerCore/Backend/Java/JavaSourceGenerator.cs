using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler.Backend.Java
{
    public abstract class JavaSourceGenerator
    {
        private CompilationContext _context;
        protected CompiledFile Source;
        protected ICompilerConfiguration Job;
        protected Scope GlobalScope;

        internal NameManager Names => _context.Names;
        internal TypeManager Types => _context.Types;
        internal String PackageName => $"{Job.PObservePackageName}";

        /// <summary>
        /// Constructs a new Java source generator for a particular output file.
        /// </summary>
        /// <param name="filename"></param>
        internal JavaSourceGenerator(ICompilerConfiguration job, string filename)
        {
            Job = job;
            Source = new CompiledFile(filename);
        }

        private void Initialize(CompilationContext ctx, Scope scope)
        {
            _context = ctx;
            GlobalScope = scope;
        }

        protected virtual void WriteFileHeader()
        {
            WriteLine($"package {PackageName};");

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
        /// <param name="ctx"></param>
        /// <param name="scope"></param>
        /// <returns>The single compiled file.</returns>
        internal IEnumerable<CompiledFile> GenerateCode(CompilationContext ctx, Scope scope)
        {
            Initialize(ctx, scope);
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
                case TypeManager.JType.JMachine _:
                    writeTermToBeCloned();
                    break;

                /* Same with immutable types. */
                case TypeManager.JType.JString _:
                case TypeManager.JType.JEnum _:
                    writeTermToBeCloned();
                    break;

                /* Collections and `any` types must be explicitly cloned with the Java P runtime's `deepClone` method;
                 * there is one override for each of these. */
                case TypeManager.JType.JAny _:
                case TypeManager.JType.JMap _:
                case TypeManager.JType.JList _:
                case TypeManager.JType.JSet _:
                    Write($"{Constants.PrtDeepCloneMethodName}(");
                    writeTermToBeCloned();
                    Write(")");
                    break;

                /* JNamedTuples and foreign types extend prt.values.PValue, and thus have an explicit `.deepEquals()`
                 * method.  (We could have just as easily passed these to the runtime's `deepClone` method, but this
                 * saves us a type dispatch). */
                case TypeManager.JType.JForeign _:
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

    static class EnumerableExtensions
    {

        /// <summary>
        /// Given some sequence and a separator value, emit a sequence such that on the first iteration
        /// the prefix is the empty string, and on all other iterations it is the supplied prefix.  We
        /// can use this for emitting values from `it` where `prefix` is a separator string.  For instance,
        ///
        /// `foreach (sep, i) in [1,2,3].withPrefixSep(",") { Console.Out.Write(sep + i) }` would emit `"1,2,3"`.
        ///
        /// </summary>
        /// <param name="prefix">The separator value.</param>
        /// <param name="it">The sequence of Ts to iterate through</param>
        /// <typeparam name="T">The type of the values in it.</typeparam>
        /// <returns>A sequence of (string, T) pairs, where the string is either the empty string or the separator.</returns>
        internal static IEnumerable<(string, T)> WithPrefixSep<T>(this IEnumerable<T> it, string prefix)
        {
            return it.Select((val, i) => (i > 0 ? prefix : "", val));
        }

        /// <summary>
        /// Given some sequence and a separator value, emit a sequence such that on the final iteration
        /// the suffix is the empty string, and on all other iterations it is the supplied suffix.  We
        /// can use this for emitting values from `it` where `suffix` is a separator string.  For instance,
        ///
        /// `foreach (i, sep) in [1,2,3].withPostfixSep(",") { Console.Out.Write(i + sep) }` would emit `"1,2,3"`.
        ///
        /// </summary>
        /// <param name="prefix">The separator value.</param>
        /// <param name="it">The sequence of Ts to iterate through</param>
        /// <typeparam name="T">The type of the values in it.</typeparam>
        /// <returns>A sequence of (T, string) pairs, where the string is either the empty string or the separator.</returns>
        internal static IEnumerable<(T, string)> WithPostfixSep<T>(this IEnumerable<T> it, string suffix)
        {
            var len = it.Count();
            return it.Select((val, i) => (val, i < len - 1 ? suffix : ""));
        }
    }
}
