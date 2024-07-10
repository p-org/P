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
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["int", "int"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["int"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["long", "long"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["long"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["boolean"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["boolean", "boolean"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["String"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["String", "String"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["Object", "Object"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["JSONArrayOfint", "JSONArrayOfint"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["JSONArrayOfint", "int"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["JSONArrayOfString", "JSONArrayOfString"]));
            TemplateNames.Add(GenerateForallTemplate(QuantifiedEvents.Count, ["JSONArrayOfString", "String"]));
            WriteLine("}");
        }

        private static string GenerateCoersion(string type, string value)
        {
            if (type.Contains("JSONArray"))
            {
                return $"(({CoercedType(type)})(((JSONArray)({value})).toArray()))";
            }
            return type switch {
                "String" => $"String.valueOf({value})",
                _ => $"(({type}) {value})"
            };
        }

        private static string ContentTypeOf(string type)
        {
            if (type.StartsWith("JSONArrayOf"))
            {
                return type.Replace("JSONArrayOf", "");
            }
            return type;
        }

        private static string CoercedType(string typeName)
        {
            if (typeName.StartsWith("JSONArrayOf"))
            {
                return typeName.Replace("JSONArrayOf", "") + "[]";
            }
            return typeName;
        }

        private string GenerateForallTemplate(int numQuantifier, string[] fieldTypes)
        {
            string templateName = $"Forall{numQuantifier}Events{string.Join("", fieldTypes)}";
            WriteLine($"public static class {templateName} {{");
            foreach (var (ty, i) in fieldTypes.Select((val, index) => (CoercedType(val), index)))
            {
                WriteLine($"private {ty} f{i};");
            }
            WriteLine($"public {templateName} ({string.Join(", ", fieldTypes.Select((val, index) => $"{CoercedType(val)} f{index}"))}) {{");
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
            // WriteLine($"boolean result = ;");
            WriteLine($"if (!({Job.ProjectName}.conjoin(predicates, arguments))) continue;");
            List<string> parameters = [];
            int c = 0;
            foreach (var (ty, i) in fieldTypes.Select((val, index) => (val, index)))
            {
                if (ty.StartsWith("JSONArrayOf"))
                {
                    var paramName = $"p{c++}";
                    var arrName = $"arr{c++}";
                    var contentType = ContentTypeOf(ty);
                    WriteLine($"JSONArray {arrName} = (JSONArray) {Job.ProjectName}.termOf(terms.get({i}), arguments);");
                    WriteLine($"{contentType}[] {paramName} = new {contentType}[{arrName}.size()];");
                    WriteLine($"for (int i = 0; i < {arrName}.size(); ++i) {{");
                    WriteLine($"{paramName}[i] = ({contentType}) {arrName}.get(i);");
                    WriteLine("}");
                    parameters.Add(paramName);
                } else {
                    parameters.Add(GenerateCoersion(ty, $"{Job.ProjectName}.termOf(terms.get({i}), arguments)"));
                }
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