using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    /// <summary>
    /// Generates Java-language stubs for foreign types and foreign functions. The layout is as follows:
    ///
    /// Each Machine scope is contained in its own class, named after the machine itself, in the
    /// `PForeign` package.  This is to hew as closely as possible to the C# code generator that weaves
    /// in FFI definitions into Machine class itself (defined as a partial class).  We don't have that
    /// functionality here, so `PGenerated.SomeMachine`'s FFI stuff will be found in `PForeign.SomeMachine`.
    /// The code generator knows how to deal with this.
    ///
    /// Top-level definitions are placed in their own special class, defined in Constants.FFIGLobalScopeCname.
    /// Foreign type stubs are created as classes for each type, named after the type, in the `PForeign.types`
    /// subpackage.
    ///
    /// This code generator only emits the above stubs into a single file, `PGenerated/FFIStubs.txt`.  The
    /// developer has to, currently, manually organise them by package under `PForeign/`, like below:
    ///
    /// nathta@bcd0741cf59d pprojtest % tree PForeign
    /// PForeign
    /// ├── Monitor.java
    /// ├── P_TopScope.java
    /// └── types
    ///     └── UnboundedInt.java
    /// </summary>
    internal class FFIStubGenerator : JavaSourceGenerator
    {

        internal FFIStubGenerator(ICompilerConfiguration job, string filename) : base(job, filename)
        {
        }

        protected override void WriteFileHeader()
        {
            WriteLine(Constants.FFICommentDivider);

            foreach (var line in Constants.FfiBanner)
            {
                WriteLine(line);
            }

            WriteLine(Constants.FFICommentDivider);

            WriteLine();
            WriteLine();
        }

        private IEnumerable<ForeignType> resolveForeignType(Variable v)
        {
            var type = v.Type;

            // In the case where the field type is a typedef, follow
            // the typename resolution until we've found the actual type.
            while (type is TypeDefType tdef)
            {
                type = tdef.TypeDefDecl.Type;
            }

            if (type is ForeignType f)
            {
                return new[] { f };
            }

            return new ForeignType[] { };
        }

        /// <summary>
        /// Extracts all the foreign types used by monitors in this program: namely, foreign types as fields
        /// in some monitor, or, foreign types used in the body of a monitor method.
        /// </summary>
        private IEnumerable<ForeignType> ExtractForeignTypesFromMonitors()
        {
            return GlobalScope.Machines.Where(m => m.IsSpec)
                .SelectMany(ExtractForeignTypesFrom)
                .ToHashSet();
        }

        /// <summary>
        /// Extracts all the foreign types used by a given Function.
        /// </summary>
        ///
        /// <param name="f"></param>
        /// <returns></returns>
        private IEnumerable<ForeignType> ExtractForeignTypesFrom(Function f)
        {
            var foreignParams = f.Signature.Parameters.SelectMany(resolveForeignType);
            var foreignLocals = f.LocalVariables.SelectMany(resolveForeignType);
            return foreignLocals.Concat(foreignParams).ToHashSet();
        }

        /// <summary>
        /// Extracts all the foreign types used by a given machine.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private IEnumerable<ForeignType> ExtractForeignTypesFrom(Machine m)
        {
            var foreignFields = m.Fields.SelectMany(resolveForeignType);
            var foreignFnVars = m.Methods.SelectMany(ExtractForeignTypesFrom);
            return foreignFields.Concat(foreignFnVars).ToHashSet();
        }

        /// <summary>
        /// Extract all foreign functions called by the supplied monitor.
        /// </summary>
        private IEnumerable<Function> ExtractForeignFunctionsFrom(Machine m)
        {
            return m.Methods.Where(f => f.IsForeign);
        }

        /// <summary>
        /// Generates Java code for a given compilation job's used events.
        ///
        /// We only emit code for events that are actually used in Monitors (i.e. they
        /// appear in at least one Monitor's `observes` set.)
        /// </summary>
        protected override void GenerateCodeImpl()
        {

            foreach (var t in ExtractForeignTypesFromMonitors())
            {
                WriteForeignTypeStub(t);
            }

            WriteTopLevelForeignFunctions();

            foreach (var m in GlobalScope.Machines.Where(m => m.IsSpec))
            {
                WriteForeignFunctions(m);
            }
        }

        private void WriteFFITypesHeader(string classname)
        {
            WriteLine(Constants.AsFFIComment(
                $"Please place and complete the following class in {Constants.FFIPackage}/types/{classname}.java :"));
        }

        private void WriteFFIHeader(string monitorName)
        {
            WriteLine(Constants.AsFFIComment(
                $"Please place and complete the following class in {Constants.FFIPackage}/{monitorName}{Constants.FFILocalScopeSuffix}.java :"));
        }

        private void WriteForeignTypeStub(ForeignType t)
        {
            WriteLine(Constants.FFICommentDivider);
            WriteLine();

            WriteFFITypesHeader(t.CanonicalRepresentation);
            WriteLine();

            WriteLine($"package {PackageName};");
            WriteLine();

            var cname = t.CanonicalRepresentation;
            WriteLine($"public class {cname} implements prt.values.PValue<{cname}> {{");

            // Constructor.
            WriteLine($"public {cname}() {{ }}");
            WriteLine();

            // Override PValue interfaces.
            WriteLine("@Override");
            WriteLine($"public {cname} deepClone() {{");
            WriteLine($"throw new RuntimeException(\"{cname}#deepClone() not implemented yet!\"); // TODO");
            WriteLine("}");
            WriteLine();

            WriteLine("@Override");
            WriteLine($"public boolean deepEquals({cname} other) {{");
            WriteLine($"throw new RuntimeException(\"{cname}#deepEquals() not implemented yet!\"); // TODO");
            WriteLine("}");
            WriteLine();

            // Override default j.l.Object methods.
            WriteLine("@Override");
            WriteLine("public int hashCode() {");
            WriteLine($"throw new RuntimeException(\"{cname}#hashCode() not implemented yet!\"); // TODO");
            WriteLine("}");
            WriteLine();

            WriteLine("@Override");
            WriteLine("public boolean equals(Object o) {");
            WriteLine("if (this == o) return true;");
            WriteLine("if (o == null || getClass() != o.getClass()) return false;");
            WriteLine($"{cname} that = ({cname}) o;");
            WriteLine("return deepEquals(that);");
            WriteLine("}");


            WriteLine("}");
            WriteLine();
        }

        /// <summary>
        /// Emits all foreign function stubs defined at the top-level program scope (i.e. not within
        /// a Machine definition).
        /// </summary>
        private void WriteTopLevelForeignFunctions()
        {
            WriteLine(Constants.FFICommentDivider);

            WriteLine();
            WriteFFIHeader(Constants.FFIGlobalScopeCname);
            WriteLine();

            WriteLine($"package {PackageName};");
            WriteLine();

            WriteLine("import prt.exceptions.*;");

            // The foreign types we need to import are the ones occuring in the signature of
            // a top-level foreign function.
            var ffs = GlobalScope.Functions.Where(f => f.IsForeign);
            foreach (var t in ffs.SelectMany(ExtractForeignTypesFrom))
            {
                var toImport = Types.JavaTypeFor(t);
                WriteLine($"import {PackageName}.{toImport.TypeName};");
            }
            WriteLine();

            // Unlike Machine-scoped functions, top-level scoped ones need to go in a special
            // class, `PForeign.P_TopScope`.
            WriteLine($"public class {Constants.FFIGlobalScopeCname} {{");
            foreach (var f in GlobalScope.Functions.Where(f => f.IsForeign))
            {
                WriteForeignFunctionStub(f);
            }

            WriteLine("}");
            WriteLine();
        }

        /// <summary>
        /// Writes all foreign function stubs for a particular machine.  If no foreign
        /// functions are defined at this scope, this is a no-op.
        /// </summary>
        /// <param name="m">The machine.</param>
        private void WriteForeignFunctions(Machine m)
        {
            var ffs = ExtractForeignFunctionsFrom(m);
            if (!ffs.Any())
            {
                return;
            }

            var mname = Names.GetNameForDecl(m);

            WriteLine(Constants.FFICommentDivider);

            WriteLine();
            WriteFFIHeader(mname);
            WriteLine();

            WriteLine($"package {PackageName};");
            WriteLine();

            WriteLine("import prt.exceptions.*;");

            // Import dependencies for a FFI bridge for a monitor are whatever foreign types are used
            // by foreign functions in this monitor.
            foreach (var t in ffs.SelectMany(ExtractForeignTypesFrom))
            {
                var toImport = Types.JavaTypeFor(t);
                WriteLine($"import {PackageName}.{toImport.TypeName};");
            }
            WriteLine();

            // Class definition: By convention, this "para-class" has the same name as
            // the P machine it is defined within, to mimic the C#-style partial class mixin
            // functionalty that we are not afforded in Java, unfortunately.
            WriteLine($"public class {mname}{Constants.FFILocalScopeSuffix} {{");
            foreach (var f in ffs)
            {
                WriteForeignFunctionStub(f, m);
            }

            WriteLine("}");
            WriteLine();
        }

        /// <summary>
        /// Write a foreign function stub for the given function f, contained within an optional
        /// Machine scope.  (The latter is used to emit a non-wildcard type parameter for the
        /// State enum type.)
        /// </summary>
        /// <param name="f">The function.</param>
        /// <param name="m">The machine scope (if null, treated as the global scope)</param>
        void WriteForeignFunctionStub(Function f, Machine m = null)
        {
            var fname = Names.GetNameForDecl(f);
            var ret = Types.JavaTypeFor(f.Signature.ReturnType);

            Write($"public static {ret.TypeName} {fname}(");
            if (f.Signature.Parameters.Any())
            {
                WriteLine();
            }

            // If the function is being emitted at global scope, any monitor can call it
            // and hence we can't say anything about the monitor's state enum type, so we
            // have to wildcard it.  Within a Machine scope, though, we know exactly what
            // type the enclosing machine's state enum type is (it's always the `PrtStates`
            // enum inner class) so we can be as precise as we need to be.
            var monitorType = (m == null
                ? "prt.Monitor<?>"
                : $"{Constants.MachineNamespaceName}.{m.Name}");

            // All foreign functions have an implicit first argument to the current machine
            Write($"Object machine");

            foreach (var param in f.Signature.Parameters)
            {
                var pname = Names.GetNameForDecl(param);
                var ptype = Types.JavaTypeFor(param.Type);

                Write($", {ptype.TypeName} {pname}");
            }

            WriteLine(") {");
            WriteLine(" /* throws RaiseEventException, TransitionException */");

            if (ret is TypeManager.JType.JVoid _)
            {
                WriteLine($"// TODO");
            }
            else
            {
                WriteLine($"return {ret.DefaultValue}; // TODO");
            }
            WriteLine("}");
            WriteLine();
        }
    }
}