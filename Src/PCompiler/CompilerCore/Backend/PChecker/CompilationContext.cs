using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.CSharp
{
    internal class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilerConfiguration job)
            : base(job)
        {
            Names = new PCheckerNameManager("PGEN_");

            FileName = $"{ProjectName}.cs";
            GlobalFunctionClassName = "GlobalFunctions";
        }

        public PCheckerNameManager Names { get; }

        /// <summary>
        /// Field-name arrays of named tuple types, keyed by the C# array initializer, so that every
        /// tuple of a type shares one array instead of allocating it on construction.
        /// </summary>
        public Dictionary<string, string> FieldNameArrays { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Foreign functions and every function that transitively calls one. Foreign code can
        /// raise or goto through the machine, so calls to these functions may leave a pending
        /// transition. It can also send or create, so the (non-foreign) callers are async.
        /// </summary>
        public HashSet<Function> ForeignCallers { get; set; } = new HashSet<Function>();

        /// <summary>
        /// True while writing code that runs in a spec monitor: a monitor method, or the monitor
        /// overload of a global function. Monitors cannot send or create, and their code is
        /// synchronous, so foreign calls there take no scheduling points.
        /// </summary>
        public bool WritingMonitorCode { get; set; }

        public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;

        public string GlobalFunctionClassName { get; }

        public string FileName { get; }

        public string GetStaticMethodQualifiedName(Function function)
        {
            return $"{GlobalFunctionClassName}.{Names.GetNameForDecl(function)}";
        }
    }
}