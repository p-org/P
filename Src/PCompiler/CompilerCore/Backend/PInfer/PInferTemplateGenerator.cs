using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    public class PInferTemplateGenerator : JavaSourceGenerator
    {
        private readonly List<PEvent> QuantifiedEvents;
        private readonly HashSet<IPExpr> Predicates;
        private readonly IDictionary<IPExpr, HashSet<Variable>> FreeEvents;
        private readonly List<IPExpr> Terms;
        public readonly List<string> TemplateNames;
        public PInferTemplateGenerator(ICompilerConfiguration job, string filename, List<PEvent> quantifiedEvents,
                HashSet<IPExpr> predicates, IEnumerable<IPExpr> terms, IDictionary<IPExpr, HashSet<Variable>> freeEvents) : base(job, filename)
        {
            QuantifiedEvents = quantifiedEvents;
            Predicates = predicates;
            Terms = [.. terms];
            FreeEvents = freeEvents;
            TemplateNames = [];
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine("public class Templates {");
            // Forall-only template
            // Two Quantifier Two Fields
            TemplateNames.Add(GenerateForallTemplate(2, ["int", "int"]));
            TemplateNames.Add(GenerateForallTemplate(2, ["int"]));
            TemplateNames.Add(GenerateForallTemplate(1, ["int", "int"]));
            TemplateNames.Add(GenerateForallTemplate(1, ["int"]));
            WriteLine("}");
        }

        private string GenerateCoersion(string type, string value)
        {
            return type switch {
                "String" => $"String.valueOf({value})",
                _ => $"(({type}) {value})"
            };
        }

        private string GenerateForallTemplate(int numQuantifier, string[] fieldTypes)
        {
            string templateName = $"Forall{numQuantifier}Events{string.Join("", fieldTypes)}";
            WriteLine($"public static class {templateName} {{");
            foreach (var (ty, i) in fieldTypes.Select((val, index) => (val, index)))
            {
                WriteLine($"private {ty} f{i};");
            }
            WriteLine($"public {templateName} ({string.Join(", ", fieldTypes.Select((val, index) => $"{val} f{index}"))}) {{");
            foreach (var (ty, i) in fieldTypes.Select((val, index) => (val, index)))
            {
                WriteLine($"this.f{i} = f{i};");
            }
            WriteLine("}");
            WriteLine($"public static void execute(List<PEvents.EventBase> trace, List<{Job.ProjectName}.PredicateWrapper> predicates, List<String> terms) {{");
            for (var i = 0; i < numQuantifier; ++i)
            {
                WriteLine($"for (PEvents.EventBase e{i}: trace) {{");
                WriteLine($"if (!(e{i} instanceof {Constants.EventNamespaceName}.{QuantifiedEvents[i].Name})) continue;");
            }
            WriteLine($"PEvents.EventBase[] arguments = {{ {string.Join(", ", Enumerable.Range(0, numQuantifier).Select(i => $"e{i}"))} }};");
            WriteLine("try {");
            WriteLine($"boolean result = {Job.ProjectName}.conjoin(predicates, arguments);");
            WriteLine("if (!result) continue;");
            WriteLine($"new {templateName}({string.Join(", ", fieldTypes.Select((ty, index) => $"{GenerateCoersion(ty, $"{Job.ProjectName}.termOf(terms.get({index}), arguments)")}"))});");
            WriteLine("} catch (Exception e) { continue; }");

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