using Plang.Compiler.TypeChecker.AST.Declarations;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    internal class TypesGenerator : JavaSourceGenerator {

        internal TypesGenerator(string filename) : base(filename)
        {
        }

        /// <summary>
        /// Generates Java code for a given compilation job's user-defined types like enums
        /// and tuples.
        /// </summary>
        protected override void GenerateCodeImpl()
        {
            WriteLine($"public class {Constants.TypesNamespaceName} {{");

            if (GlobalScope.Enums.Any())
            {
                WriteLine("/* Enums */");
                WriteLine();
                foreach (var e in GlobalScope.Enums)
                {
                    WriteEnumDecl(e);
                }
                WriteLine();
            }

            if (GlobalScope.Tuples.Any())
            {
                WriteLine("/* Tuples */");
                WriteLine();
                foreach (var t in GlobalScope.Tuples)
                {
                    WriteNamedTupleDecl(t);
                }
                WriteLine();
            }

            WriteLine("}");
        }

        private void WriteEnumDecl(PEnum e)
        {
            WriteLine($"public enum {e.Name} {{");

            int numFields = e.Values.Count();
            foreach (var (param, sep) in e.Values.Select((pair, i) => (pair, i < numFields - 1? "," : ";")))
            {
                WriteLine($"{param.Name}({param.Value.ToString()}){sep}");
            }

            WriteLine("private final int value;");
            WriteLine($"{e.Name}(int i) {{ value = i; }}");

            WriteLine("}");
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

                TypeManager.JType jType = Types.JavaTypeFor(type);

                fields.Add((jType, name));
            }

            string tname = Names.NameForNamedTuple(t);
            WriteLine($"// {t.CanonicalRepresentation}");
            WriteLine($"public static class {tname} implements {Constants.PValueClass}<{tname}> {{");

            // Write the fields.
            foreach (var (jType, fieldName) in fields)
            {
                WriteLine($"public final {jType.TypeName} {fieldName};");
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
            foreach (var (sep, (jType, fieldName)) in fields.WithPrefixSep(", "))
            {
                Write($"{sep}{jType.TypeName} {fieldName}");
            }
            WriteLine($") {{");
            foreach (var (_, fieldName) in fields)
            {
                WriteLine($"this.{fieldName} = {fieldName};");
            }
            WriteLine($"}}");
            WriteLine();

            // Write the copy constructor for cloning.
            WriteLine($"public {tname} deepClone() {{");
            Write($"return new {tname}(");
            foreach (var (sep, (jType, fieldName)) in fields.WithPrefixSep(", "))
            {
                Write(sep);
                WriteClone(jType, () => Write(fieldName));
            }
            WriteLine(");");
            WriteLine("} // deepClone()");
            WriteLine();

            // .equals() implementation: this simply defers to deepEquals() but explicitly overriding it is useful
            // for calling assertEquals() in unit tests, for example.
            WriteLine($"public boolean equals(Object other) {{");
            WriteLine("return (this.getClass() == other.getClass() && ");
            WriteLine($"this.deepEquals(({tname})other)");
            WriteLine(");");
            WriteLine("} // equals()");
            WriteLine();

            // Deep equality predicate.
            WriteLine($"public boolean deepEquals({tname} other) {{");
            WriteLine("return (true");
            foreach (var (jType, fieldName) in fields)
            {
                Write(" && ");
                WriteLine(jType.IsPrimitive
                    ? $"this.{fieldName} == other.{fieldName}"
                    : $"{Constants.PrtDeepEqualsMethodName}(this.{fieldName}, other.{fieldName})");
            }
            WriteLine(");");
            WriteLine("} // deepEquals()");
            WriteLine();

            // Write toString() in the same output style as a Java record.
            WriteLine("public String toString() {");
            WriteLine($"StringBuilder sb = new StringBuilder(\"{tname}\");");
            WriteLine("sb.append(\"[\");");
            foreach (var (sep, (_, fieldName)) in fields.WithPrefixSep(", "))
            {
                WriteLine($"sb.append(\"{sep}{fieldName}=\" + {fieldName});");
            }
            WriteLine("sb.append(\"]\");");
            WriteLine("return sb.toString();");
            WriteLine("} // toString()");

            WriteLine($"}} //{tname} class definition");

            WriteLine();
       }

    }
}
