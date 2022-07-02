using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System.Linq;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    internal class FFIStubGenerator : JavaSourceGenerator
    {

        internal FFIStubGenerator(string filename) : base(filename)
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
                PLanguageType type = v.Type;

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

                return new ForeignType[] {} ;
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
        /// Extracts all the foreign types used by a given machine.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private IEnumerable<ForeignType> ExtractForeignTypesFrom(Machine m)
        {
            IEnumerable<ForeignType> foreignFields =
                    m.Fields.Where(f => f.Type is ForeignType _)
                    .SelectMany(resolveForeignType);

            IEnumerable<ForeignType> foreignFnVars =
                    m.Methods
                    .SelectMany(f => f.LocalVariables)
                    .SelectMany(resolveForeignType);

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

            foreach (Machine m in GlobalScope.Machines.Where(m => m.IsSpec))
            {
                WriteForeignFunctions(m);
            }
        }

        private void WriteFFIGlobalHeader(string classname)
        {
            WriteLine(Constants.AsFFIComment($"Please place and complete the following class in {Constants.FFIPackage}/globals/{classname}.java :"));
        }

        private void WriteFFIHeader(string monitorName)
        {
            WriteLine(Constants.AsFFIComment($"Please place and complete the following class in {Constants.FFIPackage}/{monitorName}.java :"));
        }

        private void WriteForeignTypeStub(ForeignType t)
        {
            WriteLine(Constants.FFICommentDivider);
            WriteLine();

            WriteFFIGlobalHeader(t.CanonicalRepresentation);
            WriteLine();

            WriteLine($"package {Constants.GlobalFFIPackage};");
            WriteLine();

            string cname = t.CanonicalRepresentation;
            WriteLine($"public class {cname} implements prt.values.PValue<{cname}> {{");

            // Constructor.
            WriteLine($"public {cname}() {{ }}");
            WriteLine();

            // PValue interfaces.
            WriteLine($"public {cname} deepClone() {{");
            WriteLine($"throw new RuntimeException(\"{cname}#deepClone() not implemented yet!\"); // TODO");
            WriteLine("}");
            WriteLine();

            WriteLine($"public boolean deepEquals({cname} other) {{");
            WriteLine($"throw new RuntimeException(\"{cname}#deepEquals() not implemented yet!\"); // TODO");
            WriteLine("}");
            WriteLine();

            WriteLine("}");
            WriteLine();
        }

        private void WriteForeignFunctions(Machine m)
        {
            IEnumerable<Function> ffs = ExtractForeignFunctionsFrom(m);
            if (!ffs.Any())
            {
                return;
            }

            WriteLine(Constants.FFICommentDivider);

            WriteLine();
            WriteFFIHeader(m.Name);
            WriteLine();

            WriteLine($"package {Constants.FFIPackage};");
            WriteLine();

            // Any foreign types that this function depends on should be imported too.
            foreach (ForeignType t in ExtractForeignTypesFrom(m))
            {
                TypeManager.JType toImport = Types.JavaTypeFor(t);
                WriteLine($"import java.util.*;"); // To avoid having to fully-qualify e.g. j.u.HashMap, etc.
                WriteLine($"import {Constants.GlobalFFIPackage}.{toImport.TypeName};");
                WriteLine();
            }

            // Class definition: By convention, this "para-class" has the same name as
            // the P machine that imports it, to mimic the C#-style partial class mixin
            // functionalty that we are not afforded in Java, unfortunately.
            WriteLine($"public class {m.Name} {{");
            foreach (Function f in ffs)
            {
                TypeManager.JType ret = Types.JavaTypeFor(f.Signature.ReturnType);

                WriteLine($"public static {ret.TypeName} {f.Name}(");

                foreach (var (param, sep) in f.Signature.Parameters.WithPostfixSep(","))
                {
                    string pname = param.Name;
                    TypeManager.JType ptype = Types.JavaTypeFor(param.Type);

                    WriteLine($"{ptype.TypeName} {pname}{sep}");
                }

                WriteLine(") {");
                WriteLine($"return {ret.DefaultValue}; // TODO");
                WriteLine("}");
                WriteLine();
            }
            WriteLine("}");
            WriteLine();
        }
    }
}
