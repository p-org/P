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
        public readonly HashSet<string> TemplateNames;
        public PInferTemplateGenerator(ICompilerConfiguration job, string filename, List<PEvent> quantifiedEvents,
                HashSet<IPExpr> predicates, IEnumerable<IPExpr> terms,
                IDictionary<IPExpr, HashSet<Variable>> freeEvents,
                IDictionary<IPExpr, HashSet<int>> predicateBoundedTerms,
                IDictionary<int, IPExpr> termOrderToTerms) : base(job, filename)
        {
            QuantifiedEvents = quantifiedEvents;
            Predicates = predicates;
            Terms = [.. terms];
            FreeEvents = freeEvents;
            PredicateBoundedTerms = predicateBoundedTerms;
            TermOrderToTerms = termOrderToTerms;
            TemplateNames = [];
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine("public class Templates {");
            // Forall-only template
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [PrimitiveType.Int, PrimitiveType.Int]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [PrimitiveType.Int]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [PrimitiveType.Bool]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [PrimitiveType.Bool, PrimitiveType.Bool]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [PrimitiveType.String]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [PrimitiveType.String, PrimitiveType.String]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [PrimitiveType.Any, PrimitiveType.Any]));
            var typeComparer = new TypeNameComparison(Types);
            foreach (var p in Predicates)
            {
                var terms = PredicateBoundedTerms[p].Select(x => TermOrderToTerms[x]).ToList();
                var types = terms.Select(t => t.Type).ToList();
                types.Sort(typeComparer);
                try
                {
                    TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, [.. types]));
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

        private string GenerateForallTemplate(int numQuantifier, PLanguageType[] fieldTypes)
        {
            List<string> fieldTypeNames = fieldTypes.Select(Types.SimplifiedJavaType).ToList();
            List<TypeManager.JType> fieldTypeDecls = fieldTypes.Select(Types.JavaTypeFor).ToList();
            string templateName = $"Forall{numQuantifier}Events{string.Join("", fieldTypeNames)}";
            if (TemplateNames.Contains(templateName))
            {
                return templateName;
            }
            WriteLine($"public static class {templateName} {{");
            foreach (var (ty, i) in fieldTypeDecls.Select((val, index) => (val.TypeName, index)))
            {
                WriteLine($"private {ty} f{i};");
            }
            WriteLine($"public {templateName} ({string.Join(", ", fieldTypeDecls.Select((val, index) => $"{val.TypeName} f{index}"))}) {{");
            foreach (var (_, i) in fieldTypes.Select((val, index) => (val, index)))
            {
                WriteLine($"this.f{i} = f{i};");
            }
            WriteLine("}");
            WriteLine($"public static void execute(List<{Constants.PEventsClass}<?>> trace, List<{Job.ProjectName}.PredicateWrapper> predicates, List<String> terms) {{");
            for (var i = 0; i < numQuantifier; ++i)
            {
                WriteLine($"for ({Constants.PEventsClass}<?> e{i}: trace) {{");
                WriteLine($"if (!(e{i} instanceof {Constants.EventNamespaceName}.{QuantifiedEvents[i].Name})) continue;");
            }
            WriteLine($"{Constants.PEventsClass}<?>[] arguments = {{ {string.Join(", ", Enumerable.Range(0, numQuantifier).Select(i => $"e{i}"))} }};");
            WriteLine("try {");
            // WriteLine($"boolean result = ;");
            WriteLine($"if (!({Job.ProjectName}.conjoin(predicates, arguments))) continue;");
            List<string> parameters = [];
            foreach (var (ty, i) in fieldTypeDecls.Select((val, index) => (val, index)))
            {
                // if (ty.StartsWith("JSONArrayOf"))
                // {
                //     var paramName = $"p{c++}";
                //     var arrName = $"arr{c++}";
                //     var contentType = ContentTypeOf(ty);
                //     WriteLine($"JSONArray {arrName} = (JSONArray) {Job.ProjectName}.termOf(terms.get({i}), arguments);");
                //     WriteLine($"{contentType}[] {paramName} = new {contentType}[{arrName}.size()];");
                //     WriteLine($"for (int i = 0; i < {arrName}.size(); ++i) {{");
                //     WriteLine($"{paramName}[i] = ({contentType}) {arrName}.get(i);");
                //     WriteLine("}");
                //     parameters.Add(paramName);
                // } else {
                parameters.Add(GenerateCoersion(ty.TypeName, $"{Job.ProjectName}.termOf(terms.get({i}), arguments)"));
                // }
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