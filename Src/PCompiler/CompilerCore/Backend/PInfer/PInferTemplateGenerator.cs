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
        Event ConfigEvent;
        public readonly HashSet<string> TemplateNames;
        public PInferTemplateGenerator(ICompilerConfiguration job, List<Event> quantifiedEvents,
                HashSet<IPExpr> predicates, IEnumerable<IPExpr> terms,
                IDictionary<IPExpr, HashSet<PEventVariable>> freeEvents,
                IDictionary<IPExpr, HashSet<int>> predicateBoundedTerms,
                IDictionary<int, IPExpr> termOrderToTerms,
                Event configEvent) : base(job, PreambleConstants.TemplatesFileName)
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
                    if ((nTerms - j != 0) && (QuantifiedEvents.Count - i == 0)) continue;
                    if ((j != 0) && (i == 0)) continue;
                    List<PLanguageType> forallTypes = Enumerable.Range(0, nTerms - j).Select(_ => ty).ToList();
                    List<PLanguageType> existsTypes = Enumerable.Range(0, j).Select(_ => ty).ToList();
                    GenerateTemplate(QuantifiedEvents.Count - i, i, forallTypes, existsTypes);
                }
            }
        }

        private string GenerateCoersion(string type, string value)
        {
            return type switch {
                "String" => $"String.valueOf({value})",
                "long[]" => $"({Job.ProjectName}.ToPrimArr((Long[]) {value}))",
                _ => $"(({type}) {value})"
            };
        }
        private string GenerateTemplateName(int numForall, int numExists, IEnumerable<PLanguageType> forallTypes, IEnumerable<PLanguageType> existsTypes)
        {
            string forallTypeNames = string.Join("", forallTypes.Select(Types.SimplifiedJavaType));
            string existsTypeNames = string.Join("", existsTypes.Select(Types.SimplifiedJavaType));
            string templateName = "";
            templateName += $"Forall{numForall}{forallTypeNames}";
            templateName += $"Exists{numExists}{existsTypeNames}";
            return templateName;
        }

        private void WriteDefAndConstructor(string templateName, IEnumerable<TypeManager.JType> fieldTypeDecls, bool hasExists, bool existsN = false)
        {
            WriteLine($"public static class {templateName} {{");
            List<NamedTupleEntry> configConstants = [];
            if (existsN) {
                PLanguageType configType = ConfigEvent.PayloadType.Canonicalize();
                if (configType is NamedTupleType tuple)
                {
                    foreach (var entry in tuple.Fields)
                    {
                        configConstants.Add(entry);
                    }
                }
                else
                {
                    throw new Exception($"Config event {ConfigEvent.Name} should have a named-tuple type, got {configType}");
                }
            }
            List<string> existsCount = hasExists ? ["_num_e_exists_"] : [];
            WriteLine($"public static void mine_{templateName} ({string.Join(", ", existsCount.Select(x => $"int {x}").Concat(configConstants.Select(entry => $"{Types.JavaTypeFor(entry.Type, false).TypeName} {entry.Name}").Concat(fieldTypeDecls.Select((val, index) => $"{val.TypeName} f{index}"))))}) {{");
            WriteLine("return;");
            WriteLine("}");
        }

        private string[] GenerateConfigEventFieldAccess(string varName)
        {
            if (ConfigEvent.PayloadType.Canonicalize() is NamedTupleType tupleType)
            {
                return tupleType.Fields.Select(x => $"((({Constants.EventNamespaceName}.{ConfigEvent.Name}){varName}).getPayload().{x.Name})").ToArray();
            }
            else
            {
                throw new Exception($"Config event {ConfigEvent.Name} should have a named-tuple type");
            }
        }

        private void WritePrecheckIndexed(string eventType)
        {
            WriteLine($"if (!indices.indexed(trace, {eventType})) return;");
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
            if (numForall == 0 && forallTermTypes.Count > 0) return;
            if (numExists == 0 && existsTermTypes.Count > 0) return;
            string templateName = GenerateTemplateName(numForall, numExists, forallTermTypes, existsTermTypes);
            if (TemplateNames.Contains(templateName))
            {
                return;
            }
            TemplateNames.Add(templateName);
            // convert to Java types
            var forallTypeDecls = forallTermTypes.Select(x => Types.JavaTypeFor(x, false)).ToList();
            var existsTypeDecls = existsTermTypes.Select(x => Types.JavaTypeFor(x, false)).ToList();
            var fullDecls = forallTypeDecls.Concat(existsTypeDecls.Select(x => new TypeManager.JType.JList(x, false))).ToList();
            // exists-n (e.g. quorum)
            bool generateExistsN = ConfigEvent != null && numExists != 0;
            static string getEventTypeName(string e) => $"{Constants.EventNamespaceName}.{e}.class";
            void declVar(string name, string idx) => WriteLine($"{Constants.EventsClass}<?> {name} = trace.get({idx});");
            WriteDefAndConstructor(templateName, fullDecls, numExists != 0, generateExistsN);
            WriteLine($"public static void execute(TraceIndex indices, List<{Constants.EventsClass}<?>> trace, List<{Job.ProjectName}.PredicateWrapper> guards, List<{Job.ProjectName}.PredicateWrapper> filters, List<String> forallTerms, List<String> existsTerms) {{");
            if (numExists > 0)
            {
                for (int i = 0; i < existsTypeDecls.Count; ++i)
                {
                    WriteLine($"List<List<{existsTypeDecls[i].ReferenceTypeName}>> etLst{i} = new ArrayList<>();");
                }
                WriteLine($"List<{Constants.EventsClass}[]> guardsArgsLst = new ArrayList<>();");
                WriteLine($"List<Integer> numExistsLst = new ArrayList<>();");
            }
            if (generateExistsN)
            {
                var eventTypeName = getEventTypeName(ConfigEvent.Name);
                WriteLine($"{Constants.EventNamespaceName}.{ConfigEvent.Name} configEvent = null;");
                WritePrecheckIndexed(eventTypeName);
                WriteLine($"for (int cfgIdx: indices.getIndices(trace, {eventTypeName})) {{");
                declVar("eConfig", "cfgIdx");
                WriteLine($"configEvent = ({Constants.EventNamespaceName}.{ConfigEvent.Name}) eConfig;");
            }
            foreach (var name in QuantifiedEvents)
            {
                WritePrecheckIndexed(getEventTypeName(name));
            }
            for (int i = 0; i < numForall; ++i)
            {
                // forall-quantifications
                WriteLine($"for (int e{i}Idx: indices.getIndices(trace, {getEventTypeName(QuantifiedEvents[i])})) {{");
                declVar($"e{i}", $"e{i}Idx");
            }
            WriteLine("try {");
            if (numForall > 0)
            {
                WriteLine($"{Constants.EventsClass}<?>[] guardsArgs = {{ {string.Join(", ", Enumerable.Range(0, numForall).Select(i => $"e{i}"))} }};");
                WriteLine($"if (!({Job.ProjectName}.conjoin(guards, guardsArgs))) continue;");
            }
            // define aggregation arrays for each existentially quantified terms
            for (int i = 0; i < existsTypeDecls.Count; ++i)
            {
                WriteLine($"List<{existsTypeDecls[i].ReferenceTypeName}> et{i} = new ArrayList<>();");
            }
            if (numExists > 0)
            {
                WriteLine($"int numExistsComb = 0;");
            }
            for (int i = 0; i < numExists; ++i)
            {
                WriteLine($"for (int e{i + numForall}Idx: indices.getIndices(trace, {getEventTypeName(QuantifiedEvents[i + numForall])})) {{");
                declVar($"e{i + numForall}", $"e{i + numForall}Idx");
            }
            if (numExists > 0)
            {
                WriteLine($"{Constants.EventsClass}<?>[] filterArgs = {{ {string.Join(", ", Enumerable.Range(0, numForall + numExists).Select(i => $"e{i}"))} }};");
                WriteLine($"if (!({Job.ProjectName}.conjoin(filters, filterArgs))) continue;");
                WriteLine($"numExistsComb += 1;");
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
            // first, check whether any list is empty
            // Do we really need to check `numForall > 0`?
            if (numForall > 0 && numExists > 0)
            {
                WriteLine("if (numExistsComb == 0) throw new RuntimeException(\"FilterFailedAbort\");");
            }
            for (int i = 0; i < existsTermTypes.Count; ++i)
            {
                WriteLine($"etLst{i}.add(et{i});");
            }
            string[] configEventAccess = [];
            if (generateExistsN)
            {
                configEventAccess = GenerateConfigEventFieldAccess("configEvent");
            }
            // List<string> existsComb = numExists > 0 ? ["numExistsComb"] : [];
            var forallTermsInsts = forallTypeDecls.Select((x, i) => GenerateCoersion(x.TypeName, $"{Job.ProjectName}.termOf(forallTerms.get({i}), guardsArgs)")).ToList();
            if (numExists == 0)
            {
                WriteLine($"mine_{templateName}({string.Join(", ", configEventAccess.Concat(forallTermsInsts))});");
            }
            else
            {
                if (numForall > 0)
                {
                    WriteLine($"guardsArgsLst.add(guardsArgs);");
                }
                WriteLine($"numExistsLst.add(numExistsComb);");
            }
            WriteLine("} catch (Exception e) { if (e instanceof RuntimeException) throw (RuntimeException) e; }");
            for (int i = 0; i < numForall; ++i)
            {
                WriteLine("}"); // forall-quantification
            }
            if (generateExistsN)
            {
                WriteLine("}"); // config event
            }
            if (numExists > 0)
            {
                WriteLine("for (int i = 0; i < numExistsLst.size(); ++i) {");
                for (int i = 0; i < existsTermTypes.Count; ++i)
                {
                    WriteLine($"{existsTypeDecls[i].TypeName}[] et{i}Arr = new {existsTypeDecls[i].TypeName}[etLst{i}.get(i).size()];");
                    WriteLine($"for (int j = 0; j < etLst{i}.get(i).size(); ++j) {{");
                    WriteLine($"et{i}Arr[j] = etLst{i}.get(i).get(j);");
                    WriteLine("}"); // copy loop
                }
                forallTermsInsts = forallTypeDecls.Select((x, i) => GenerateCoersion(x.TypeName, $"{Job.ProjectName}.termOf(forallTerms.get({i}), guardsArgsLst.get(i))")).ToList();
                string[] existsComb = ["numExistsLst.get(i)"];
                WriteLine($"mine_{templateName}({string.Join(", ", existsComb.Concat(configEventAccess.Concat(forallTermsInsts).Concat(Enumerable.Range(0, existsTermTypes.Count).Select(i => $"et{i}Arr"))))});");
                WriteLine("}"); // forall args loop
            }
            WriteLine("}"); // execute
            WriteLine("}"); // class def
        }
    }
}
