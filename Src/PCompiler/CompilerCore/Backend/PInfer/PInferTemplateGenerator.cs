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
                GenerateForallTemplate(QuantifiedEvents.Count, args);
                GenerateExistsTemplate(QuantifiedEvents.Count, args);
                GenerateForallExistsTemplate(QuantifiedEvents.Count, args);
            }
            foreach (var tupleType in Terms.Select(x => x.Type))
            {
                GenerateForallTemplate(QuantifiedEvents.Count, [tupleType]);
                GenerateForallTemplate(QuantifiedEvents.Count, [tupleType, tupleType]);
                GenerateExistsTemplate(QuantifiedEvents.Count, [tupleType]);
                GenerateExistsTemplate(QuantifiedEvents.Count, [tupleType, tupleType]);
                GenerateForallExistsTemplate(QuantifiedEvents.Count, [tupleType]);
                GenerateForallExistsTemplate(QuantifiedEvents.Count, [tupleType, tupleType]);
            }
            var typeComparer = new TypeNameComparison(Types);
            foreach (var p in Predicates)
            {
                var terms = PredicateBoundedTerms[p].Select(x => TermOrderToTerms[x]).ToList();
                var types = terms.Select(t => t.Type).ToList();
                types.Sort(typeComparer);
                try
                {
                    GenerateForallTemplate(QuantifiedEvents.Count, [.. types]);
                    GenerateExistsTemplate(QuantifiedEvents.Count, [.. types]);
                    GenerateForallExistsTemplate(QuantifiedEvents.Count, [.. types]);
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

        private void WriteDefAndConstructor(string templateName, IEnumerable<TypeManager.JType> fieldTypeDecls, bool existsN = false)
        {
            WriteLine($"public static class {templateName} {{");
            foreach (var (ty, i) in fieldTypeDecls.Select((val, index) => (val.TypeName, index)))
            {
                WriteLine($"private {ty} f{i};");
            }
            List<NamedTupleEntry> configConstants = [];
            if (existsN) {
                PLanguageType configType = ConfigEvent.PayloadType;
                if (configType is NamedTupleType tuple)
                {
                    foreach (var entry in tuple.Fields)
                    {
                        var jType = Types.JavaTypeFor(entry.Type);
                        WriteLine($"private {jType.TypeName} {entry.Name};");
                        configConstants.Add(entry);
                    }
                }
                else
                {
                    throw new Exception($"Config event {ConfigEvent.Name} should have a named-tuple type");
                }
            }
            WriteLine($"public {templateName} ({string.Join(", ", configConstants.Select(entry => $"{Types.JavaTypeFor(entry.Type).TypeName} {entry.Name}").Concat(fieldTypeDecls.Select((val, index) => $"{val.TypeName} f{index}")))}) {{");
            for (int i = 0; i < fieldTypeDecls.Count(); ++i)
            {
                WriteLine($"this.f{i} = f{i};");
            }
            foreach(var entry in configConstants)
            {
                WriteLine($"this.{entry.Name} = {entry.Name};");
            }
            WriteLine("}");
        }

        private string[] GenerateConfigEventFieldAccess(string varName)
        {
            if (ConfigEvent.PayloadType is NamedTupleType tupleType)
            {
                return tupleType.Fields.Select(x => $"((({Constants.EventNamespaceName}.{ConfigEvent.Name}){varName}).getPayload().{x.Name})").ToArray();
            }
            else
            {
                throw new Exception($"Config event {ConfigEvent.Name} should have a named-tuple type");
            }
        }

        private void GenerateForallExistsTemplate(int numQuantifier, PLanguageType[] fieldTypes)
        {
            // assumption: the first term is related to the existentially quantified event and
            //             is of a primitive type.
            // other terms are not related to the existentially quantified events.
            string templateName = GenerateTemplateName("ForallExists", numQuantifier, fieldTypes);
            TypeManager.JType[] fieldTypeDecls = fieldTypes.Select(Types.JavaTypeFor).ToArray();
            if (!fieldTypeDecls[0].IsPrimitive || numQuantifier < 2)
            {
                return;
            }
            if (TemplateNames.Contains(templateName))
            {
                return;
            }
            bool generateExistsN = ConfigEvent != null;
            TemplateNames.Add(templateName);
            TypeManager.JType termArr = new TypeManager.JType.JList(fieldTypeDecls[^1]);
            TypeManager.JType[] arrayContained = [termArr];
            WriteDefAndConstructor(templateName, fieldTypeDecls.SkipLast(1).Concat(arrayContained), generateExistsN);
            WriteLine($"public static void execute(List<{Constants.PEventsClass}<?>> trace, List<{Job.ProjectName}.PredicateWrapper> predicates, List<{Job.ProjectName}.PredicateWrapper> filters, List<String> terms) {{");
            if (generateExistsN)
            {
                WriteLine($"for ({Constants.PEventsClass}<?> eConfig: trace) {{");
                WriteLine($"if (!(eConfig instanceof {Constants.EventNamespaceName}.{ConfigEvent.Name})) continue;");
            }
            for (int i = 0; i < numQuantifier - 1; ++i)
            {
                // forall-quantifications
                WriteLine($"for ({Constants.PEventsClass}<?> e{i}: trace) {{");
                WriteLine($"if (!(e{i} instanceof {Constants.EventNamespaceName}.{QuantifiedEvents[i].Name})) continue;");
            }
            WriteLine("try {");
            if (numQuantifier > 1)
            {
                WriteLine($"{Constants.PEventsClass}<?>[] guardsArgs = {{ {string.Join(", ", Enumerable.Range(0, numQuantifier - 1).Select(i => $"e{i}"))} }};");
                WriteLine($"if (!({Job.ProjectName}.conjoin(predicates, guardsArgs))) continue;");
            }
            var lastIndex = numQuantifier - 1;
            // aggregation array
            WriteLine($"List<{fieldTypeDecls[0].ReferenceTypeName}> termsList = new ArrayList<>();");
            WriteLine($"for ({Constants.PEventsClass}<?> e{lastIndex}: trace) {{");
            WriteLine($"if (!(e{lastIndex} instanceof {Constants.EventNamespaceName}.{QuantifiedEvents[lastIndex].Name})) continue;");
            WriteLine($"{Constants.PEventsClass}<?>[] filterArgs = {{ {string.Join(", ", Enumerable.Range(0, numQuantifier).Select(i => $"e{i}"))} }};");
            WriteLine($"if (!({Job.ProjectName}.conjoin(filters, filterArgs))) continue;");
            WriteLine($"termsList.add({GenerateCoersion(fieldTypeDecls[^1].TypeName, $"{Job.ProjectName}.termOf(terms.getLast(), filterArgs)")});");
            WriteLine("}"); // existential quantification
            WriteLine($"{termArr.ReferenceTypeName} termsArr = new {fieldTypeDecls[^1].TypeName}[termsList.size()];");
            WriteLine("for (int i = 0; i < termsList.size(); ++i) {");
            WriteLine($"termsArr[i] = termsList.get(i);");
            WriteLine("}"); // copy loop
            string[] configEventAccess = [];
            if (generateExistsN)
            {
                configEventAccess = GenerateConfigEventFieldAccess("eConfig");
            }
            WriteLine($"new {templateName}({string.Join(", ", configEventAccess.Concat(fieldTypeDecls.SkipLast(1).Select((x, i) => GenerateCoersion(x.TypeName, $"{Job.ProjectName}.termOf(terms.get({i}), guardsArgs)"))).Concat(["termsArr"]))});");
            WriteLine("} catch (Exception e) { if (e instanceof RuntimeException) throw (RuntimeException) e; }");

            for (int i = 0; i < numQuantifier - 1; ++i)
            {
                WriteLine("}"); // forall-quantification
            }
            if (generateExistsN)
            {
                WriteLine("}"); // config event
            }
            WriteLine("}"); // execute
            WriteLine("}"); // class def
        }

        private void GenerateExistsTemplate(int numQuantifier, PLanguageType[] fieldTypes)
        {
            // assumption: selected terms are all of primitive types
            // (otherwise, Daikon cannot generate meaningful properties)
            string templateName = GenerateTemplateName("Exists", numQuantifier, fieldTypes);
            if (TemplateNames.Contains(templateName))
            {
                return;
            }
            var fieldTypeDecls = fieldTypes.Select(Types.JavaTypeFor).ToList();
            if (!fieldTypeDecls.All(x => x.IsPrimitive))
            {
                return;
            }
            TemplateNames.Add(templateName);
            var arrayContained = fieldTypeDecls.Select(x => new TypeManager.JType.JList(x)).ToArray();
            WriteDefAndConstructor(templateName, arrayContained);
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
            for (var i = 0; i < fieldTypeDecls.Count; ++i)
            {
                WriteLine($"{fieldTypeDecls[i].TypeName}[] e{i}arr = new {fieldTypeDecls[i].TypeName}[es{i}.size()];");
                WriteLine($"for (int i = 0; i < es{i}.size(); i++) {{");
                WriteLine($"e{i}arr[i] = {GenerateCoersion(fieldTypeDecls[i].TypeName, $"es{i}.get(i)")};");
                WriteLine("}");
            }
            WriteLine($"new {templateName}({string.Join(", ", Enumerable.Range(0, fieldTypes.Length).Select(n => $"e{n}arr"))});");
            WriteLine("}"); // execute
            WriteLine("}"); // class def
        }

        private void GenerateForallTemplate(int numQuantifier, PLanguageType[] fieldTypes)
        {
            string templateName = GenerateTemplateName("Forall", numQuantifier, fieldTypes);
            if (TemplateNames.Contains(templateName))
            {
                return;
            }
            var fieldTypeDecls = fieldTypes.Select(Types.JavaTypeFor).ToList();
            TemplateNames.Add(templateName);
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
        }
    }
}