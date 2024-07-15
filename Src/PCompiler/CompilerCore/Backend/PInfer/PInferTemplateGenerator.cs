using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{

    internal class TypeNameComparison : Comparer<PLanguageType>
    {
        TypeManager Types;
        public TypeNameComparison(TypeManager manager) {
            Types = manager;
        }
        public override int Compare(PLanguageType x, PLanguageType y)
        {
            return Types.SimplifiedJavaType(x).CompareTo(Types.SimplifiedJavaType(y));
        }
    }

    public class PInferTemplateGenerator : JavaSourceGenerator
    {
        private readonly List<PEvent> QuantifiedEvents;
        private readonly HashSet<IPExpr> Predicates;
        private readonly IDictionary<IPExpr, HashSet<Variable>> FreeEvents;
        private readonly List<IPExpr> Terms;
        IDictionary<IPExpr, HashSet<int>> PredicateBoundedTerms;
        IDictionary<int, IPExpr> TermOrderToTerms;
        PEvent ConfigEvent;
        public readonly HashSet<string> TemplateNames;
        public PInferTemplateGenerator(ICompilerConfiguration job, string filename, List<PEvent> quantifiedEvents,
                HashSet<IPExpr> predicates, IEnumerable<IPExpr> terms,
                IDictionary<IPExpr, HashSet<Variable>> freeEvents,
                IDictionary<IPExpr, HashSet<int>> predicateBoundedTerms,
                IDictionary<int, IPExpr> termOrderToTerms,
                PEvent configEvent) : base(job, filename)
        {
            QuantifiedEvents = quantifiedEvents;
            Predicates = predicates;
            Terms = [.. terms];
            FreeEvents = freeEvents;
            PredicateBoundedTerms = predicateBoundedTerms;
            TermOrderToTerms = termOrderToTerms;
            ConfigEvent = configEvent;
            TemplateNames = [];
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine("public class Templates {");
            // Forall-only template
            PLanguageType[][] primitiveArgs = [
                [PrimitiveType.Int, PrimitiveType.Int],
                [PrimitiveType.Int],
                [PrimitiveType.Bool],
                [PrimitiveType.Bool, PrimitiveType.Bool],
                [PrimitiveType.String],
                [PrimitiveType.String, PrimitiveType.String],
                [PrimitiveType.Any, PrimitiveType.Any]
            ];
            foreach (var args in primitiveArgs)
            {
                TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, args));
                TemplateNames.Add(GenerateExistsTemplate(QuantifiedEvents.Count, args));
            }
            foreach (var tupleType in Terms.Select(x => x.Type))
            {
                TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [tupleType]));
                TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [tupleType, tupleType]));
                TemplateNames.Add(GenerateExistsTemplate(QuantifiedEvents.Count, [tupleType]));
                TemplateNames.Add(GenerateExistsTemplate(QuantifiedEvents.Count, [tupleType, tupleType]));
            }
            var typeComparer = new TypeNameComparison(Types);
            foreach (var p in Predicates)
            {
                var terms = PredicateBoundedTerms[p].Select(x => TermOrderToTerms[x]).ToList();
                var types = terms.Select(t => t.Type).ToList();
                types.Sort(typeComparer);
                try
                {
                    TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [.. types]));
                    TemplateNames.Add(GenerateExistsTemplate(QuantifiedEvents.Count, [.. types]));
                }
                catch (NotImplementedException)
                {
                    continue;
                }
            }
            WriteLine("}");
        }

        private static string GenerateCoersion(string type, string value)
        {
            return type switch {
                "String" => $"String.valueOf({value})",
                _ => $"(({type}) {value})"
            };
        }
        private string GenerateTemplateName(string templateType, int numQuantifier, PLanguageType[] fieldTypes)
        {
            List<string> fieldTypeNames = fieldTypes.Select(Types.SimplifiedJavaType).ToList();
            List<TypeManager.JType> fieldTypeDecls = fieldTypes.Select(Types.JavaTypeFor).ToList();
            return $"{templateType}{numQuantifier}Events{string.Join("", fieldTypeNames)}";
        }

        private void WriteDefAndConstructor(string templateName, IEnumerable<TypeManager.JType> fieldTypeDecls)
        {
            WriteLine($"public static class {templateName} {{");
            foreach (var (ty, i) in fieldTypeDecls.Select((val, index) => (val.TypeName, index)))
            {
                WriteLine($"private {ty} f{i};");
            }
            WriteLine($"public {templateName} ({string.Join(", ", fieldTypeDecls.Select((val, index) => $"{val.TypeName} f{index}"))}) {{");
            for (int i = 0; i < fieldTypeDecls.Count(); ++i)
            {
                WriteLine($"this.f{i} = f{i};");
            }
            WriteLine("}");
        }

        private string GenerateExistsTemplate(int numQuantifier, PLanguageType[] fieldTypes)
        {
            string templateName = GenerateTemplateName("Exists", numQuantifier, fieldTypes);
            var fieldTypeDecls = fieldTypes.Select(Types.JavaTypeFor).ToList();
            if (TemplateNames.Contains(templateName))
            {
                return templateName;
            }
            PLanguageType eQuantified = fieldTypes[0];
            PLanguageType[] remaining = fieldTypes.Skip(1).ToArray();
            WriteDefAndConstructor(templateName, fieldTypeDecls);
            WriteLine($"public static void execute(List<{Constants.PEventsClass}<?>> trace, List<{Job.ProjectName}.PredicateWrapper> predicates, List<{Job.ProjectName}.PredicateWrapper> filters, List<String> terms) {{");
            for (int i = 0 ; i < fieldTypes.Length; ++i)
            {
                WriteLine($"List<{fieldTypeDecls[i].ReferenceTypeName}> es{i} = new ArrayList<>();");
            }
            for (var i = 0; i < numQuantifier; ++i)
            {
                WriteLine($"for ({Constants.PEventsClass}<?> e{i}: trace) {{");
                WriteLine($"if (!(e{i} instanceof {Constants.EventNamespaceName}.{QuantifiedEvents[i].Name})) continue;");
            }
            WriteLine($"{Constants.PEventsClass}<?>[] arguments = {{ {string.Join(", ", Enumerable.Range(0, numQuantifier).Select(i => $"e{i}"))} }};");
            WriteLine("try {");
            WriteLine($"if (!({Job.ProjectName}.conjoin(predicates, arguments))) continue;");
            List<string> parameters = [];
            foreach (var (ty, i) in fieldTypeDecls.Select((val, index) => (val, index)))
            {
                parameters.Add(GenerateCoersion(ty.TypeName, $"{Job.ProjectName}.termOf(terms.get({i}), arguments)"));
            }
            for (var i = 0; i < fieldTypeDecls.Count; ++i)
            {
                WriteLine($"es{i}.add({parameters[i]});");
            }
            WriteLine("} catch (Exception e) { if (e instanceof RuntimeException) throw (RuntimeException) e; }");
            for (var i = 0; i < numQuantifier; ++i)
            {
                WriteLine("}"); // for-loop
            }
            WriteLine("for (int i = 0; i < es0.size(); ++i) {");
            WriteLine($"new {templateName}({string.Join(", ", Enumerable.Range(0, fieldTypes.Length).Select(n => $"es{n}.get(i)"))});");
            WriteLine("}"); // for-loop
            WriteLine("}"); // execute
            WriteLine("}"); // class def
            return templateName;
        }

        private string GenerateForallTemplate(int numQuantifier, PLanguageType[] fieldTypes)
        {
            string templateName = GenerateTemplateName("Forall", numQuantifier, fieldTypes);
            var fieldTypeDecls = fieldTypes.Select(Types.JavaTypeFor).ToList();
            if (TemplateNames.Contains(templateName))
            {
                return templateName;
            }
            // Class def and constructors
            WriteDefAndConstructor(templateName, fieldTypeDecls);
            // Execute method
            WriteLine($"public static void execute(List<{Constants.PEventsClass}<?>> trace, List<{Job.ProjectName}.PredicateWrapper> predicates, List<{Job.ProjectName}.PredicateWrapper> ___filtersDiscarded, List<String> terms) {{");
            for (var i = 0; i < numQuantifier; ++i)
            {
                WriteLine($"for ({Constants.PEventsClass}<?> e{i}: trace) {{");
                WriteLine($"if (!(e{i} instanceof {Constants.EventNamespaceName}.{QuantifiedEvents[i].Name})) continue;");
            }
            WriteLine($"{Constants.PEventsClass}<?>[] arguments = {{ {string.Join(", ", Enumerable.Range(0, numQuantifier).Select(i => $"e{i}"))} }};");
            WriteLine("try {");
            WriteLine($"if (!({Job.ProjectName}.conjoin(predicates, arguments))) continue;");
            List<string> parameters = [];
            foreach (var (ty, i) in fieldTypeDecls.Select((val, index) => (val, index)))
            {
                parameters.Add(GenerateCoersion(ty.TypeName, $"{Job.ProjectName}.termOf(terms.get({i}), arguments)"));
            }
            WriteLine($"new {templateName}({string.Join(", ", parameters)});");
            WriteLine("} catch (Exception e) { if (e instanceof RuntimeException) throw (RuntimeException) e; }");

            for (var i = 0; i < numQuantifier; ++i)
            {
                WriteLine("}");
            }
            WriteLine("}");
            WriteLine("}");
            return templateName;
        }
    }
}