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
        private readonly List<string> QuantifiedEvents;
        private readonly List<string> EventVariableNames;
        private readonly HashSet<IPExpr> Predicates;
        private readonly IDictionary<IPExpr, HashSet<PEventVariable>> FreeEvents;
        private readonly List<IPExpr> Terms;
        private TypeNameComparison TypeNameCmp;
        IDictionary<IPExpr, HashSet<int>> PredicateBoundedTerms;
        IDictionary<int, IPExpr> TermOrderToTerms;
        PEvent ConfigEvent;
        public readonly HashSet<string> TemplateNames;
        public PInferTemplateGenerator(ICompilerConfiguration job, List<PEvent> quantifiedEvents,
                HashSet<IPExpr> predicates, IEnumerable<IPExpr> terms,
                IDictionary<IPExpr, HashSet<PEventVariable>> freeEvents,
                IDictionary<IPExpr, HashSet<int>> predicateBoundedTerms,
                IDictionary<int, IPExpr> termOrderToTerms,
                PEvent configEvent) : base(job, PreambleConstants.TemplatesFileName)
        {
            QuantifiedEvents = quantifiedEvents.Select((x, i) => x.Name).ToList();
            EventVariableNames = quantifiedEvents.Select((x, i) => $"e{i}").ToList();
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
            TypeNameCmp = new TypeNameComparison(Types);
            WriteLine("public class Templates {");
            // Forall-only template
            PLanguageType[] primitiveArgs = [
                PrimitiveType.Int,
                PrimitiveType.Bool,
                PrimitiveType.String,
                PrimitiveType.Float,
                PrimitiveType.Any
            ];
            foreach (var primType in primitiveArgs)
            {
                GenerateForSingleType(primType, 1);
            }
            foreach (var tupleType in Terms.Select(x => x.Type))
            {
                GenerateForSingleType(tupleType, 1);
            }
            foreach (var p in Predicates)
            {
                var terms = PredicateBoundedTerms[p].Select(x => TermOrderToTerms[x]).ToList();
                GenerateForTerms(terms);
            }
            WriteLine("}");
        }

        private void GenerateForTerms(List<IPExpr> terms)
        {
            HashSet<string> existsQuantifiedEvents = [];
            HashSet<string> forallQuantifiedEvents = [];
            for (int i = 0; i <= EventVariableNames.Count; ++i)
            {
                existsQuantifiedEvents = EventVariableNames.TakeLast(i).ToHashSet();
                forallQuantifiedEvents = EventVariableNames.SkipLast(i).ToHashSet();
                // determine if the term bounds existentially quantified events
                List<PLanguageType> forallTypes = [];
                List<PLanguageType> existsTypes = [];
                foreach (var term in terms)
                {
                    var boundedEvents = FreeEvents[term].Select(x => x.Name).ToHashSet();
                    if (boundedEvents.Overlaps(existsQuantifiedEvents))
                    {
                        existsTypes.Add(term.Type);
                    }
                    else
                    {
                        forallTypes.Add(term.Type);
                    }
                }
                GenerateTemplate(QuantifiedEvents.Count - i, i, forallTypes, existsTypes);
            }
        }

        private void GenerateForSingleType(PLanguageType ty, int nTerms = 2)
        {
            for (int i = 0; i <= QuantifiedEvents.Count; ++i)
            {
                for (int j = 0; j <= nTerms; ++j)
                {
                    if ((nTerms - j == 0) ^ (QuantifiedEvents.Count - i == 0)) continue;
                    if ((j == 0) ^ (i == 0)) continue;
                    List<PLanguageType> forallTypes = Enumerable.Range(0, nTerms - j).Select(_ => ty).ToList();
                    List<PLanguageType> existsTypes = Enumerable.Range(0, j).Select(_ => ty).ToList();
                    GenerateTemplate(QuantifiedEvents.Count - i, i, forallTypes, existsTypes);
                }
            }
        }

        private static string GenerateCoersion(string type, string value)
        {
            return type switch {
                "String" => $"String.valueOf({value})",
                _ => $"(({type}) {value})"
            };
        }
        private string GenerateTemplateName(int numForall, int numExists, IEnumerable<PLanguageType> forallTypes, IEnumerable<PLanguageType> existsTypes)
        {
            string forallTypeNames = string.Join("", forallTypes.Select(Types.SimplifiedJavaType));
            string existsTypeNames = string.Join("", existsTypes.Select(Types.SimplifiedJavaType));
            string templateName = "";
            if (forallTypeNames.Length != 0)
            {
                templateName += $"Forall{numForall}{forallTypeNames}";
            }
            if (existsTypeNames.Length != 0)
            {
                templateName += $"Exists{numExists}{existsTypeNames}";
            }
            return templateName;
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

        private void GenerateTemplate(int numForall, int numExists,
                            List<PLanguageType> forallTermTypes,
                            List<PLanguageType> existsTermTypes)
        {
            if (existsTermTypes.Any(x => !Types.JavaTypeFor(x).IsPrimitive))
            {
                // TODO: do we need to handle array of non-primitive types?
                return;
            }
            forallTermTypes.Sort(TypeNameCmp);
            existsTermTypes.Sort(TypeNameCmp);
            string templateName = GenerateTemplateName(numForall, numExists, forallTermTypes, existsTermTypes);
            if (TemplateNames.Contains(templateName))
            {
                return;
            }
            TemplateNames.Add(templateName);
            // convert to Java types
            var forallTypeDecls = forallTermTypes.Select(Types.JavaTypeFor).ToList();
            var existsTypeDecls = existsTermTypes.Select(Types.JavaTypeFor).ToList();
            var fullDecls = forallTypeDecls.Concat(existsTypeDecls.Select(x => new TypeManager.JType.JList(x))).ToList();
            // exists-n (e.g. quorum)
            bool generateExistsN = ConfigEvent != null && numExists != 0;
            WriteDefAndConstructor(templateName, fullDecls, generateExistsN);
            WriteLine($"public static void execute(List<{Constants.PEventsClass}<?>> trace, List<{Job.ProjectName}.PredicateWrapper> guards, List<{Job.ProjectName}.PredicateWrapper> filters, List<String> forallTerms, List<String> existsTerms) {{");
            if (generateExistsN)
            {
                WriteLine($"for ({Constants.PEventsClass}<?> eConfig: trace) {{");
                WriteLine($"if (!(eConfig instanceof {Constants.EventNamespaceName}.{ConfigEvent.Name})) continue;");
            }
            for (int i = 0; i < numForall; ++i)
            {
                // forall-quantifications
                WriteLine($"for ({Constants.PEventsClass}<?> e{i}: trace) {{");
                WriteLine($"if (!(e{i} instanceof {Constants.EventNamespaceName}.{QuantifiedEvents[i]})) continue;");
            }
            WriteLine("try {");
            if (numForall > 0)
            {
                WriteLine($"{Constants.PEventsClass}<?>[] guardsArgs = {{ {string.Join(", ", Enumerable.Range(0, numForall).Select(i => $"e{i}"))} }};");
                WriteLine($"if (!({Job.ProjectName}.conjoin(guards, guardsArgs))) continue;");
            }
            // define aggregation arrays for each existentially quantified terms
            for (int i = 0; i < existsTypeDecls.Count; ++i)
            {
                WriteLine($"List<{existsTypeDecls[i].ReferenceTypeName}> et{i} = new ArrayList<>();");
            }
            for (int i = 0; i < numExists; ++i)
            {
                WriteLine($"for ({Constants.PEventsClass}<?> e{i + numForall}: trace) {{");
                WriteLine($"if (!(e{i + numForall} instanceof {Constants.EventNamespaceName}.{QuantifiedEvents[i + numForall]})) continue;");
            }
            if (numExists > 0)
            {
                WriteLine($"{Constants.PEventsClass}<?>[] filterArgs = {{ {string.Join(", ", Enumerable.Range(0, numForall + numExists).Select(i => $"e{i}"))} }};");
                WriteLine($"if (!({Job.ProjectName}.conjoin(filters, filterArgs))) continue;");
            }
            // aggregate existentially quantified terms
            for (int i = 0; i < existsTermTypes.Count; ++i)
            {
                WriteLine($"et{i}.add({GenerateCoersion(existsTypeDecls[i].TypeName, $"{Job.ProjectName}.termOf(existsTerms.get({i}), filterArgs)")});");
            }
            for (int i = 0; i < numExists; ++i)
            {
                WriteLine("}"); // existential quantifications
            }
            // convert to array
            for (int i = 0; i < existsTermTypes.Count; ++i)
            {
                WriteLine($"{existsTypeDecls[i].TypeName}[] et{i}Arr = new {existsTypeDecls[i].TypeName}[et{i}.size()];");
                WriteLine($"for (int i = 0; i < et{i}.size(); ++i) {{");
                WriteLine($"et{i}Arr[i] = et{i}.get(i);");
                WriteLine("}"); // copy loop
            }
            string[] configEventAccess = [];
            if (generateExistsN)
            {
                configEventAccess = GenerateConfigEventFieldAccess("eConfig");
            }
            var forallTermsInsts = forallTypeDecls.Select((x, i) => GenerateCoersion(x.TypeName, $"{Job.ProjectName}.termOf(forallTerms.get({i}), guardsArgs)")).ToList();
            WriteLine($"new {templateName}({string.Join(", ", configEventAccess.Concat(forallTermsInsts).Concat(Enumerable.Range(0, existsTermTypes.Count).Select(i => $"et{i}Arr")))});");
            WriteLine("} catch (Exception e) { if (e instanceof RuntimeException) throw (RuntimeException) e; }");
            for (int i = 0; i < numForall; ++i)
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
    }
}