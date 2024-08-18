using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{
    public class PInferPredicateGenerator : ICodeGenerator
    {

        public bool HasCompilationStage => true;
        public Hint hint = null;

        public PInferPredicateGenerator()
        {
            Terms = [];
            var comparer = new ASTComparer();
            VisitedSet = new HashSet<IPExpr>(comparer);
            Predicates = new HashSet<IPExpr>(comparer);
            FreeEvents = new Dictionary<IPExpr, HashSet<PEventVariable>>(comparer);
            TermOrder = new Dictionary<IPExpr, int>(comparer);
            OrderToTerm = new Dictionary<int, IPExpr>();
            PredicateBoundedTerm = new Dictionary<IPExpr, HashSet<int>>(comparer);
            PredicateOrder = new Dictionary<IPExpr, int>(comparer);
            Contradictions = new Dictionary<IPExpr, HashSet<IPExpr>>(comparer);
        }

        public void WithHint(Hint h)
        {
            hint = h;
        }

        public void Compile(ICompilerConfiguration job)
        {
            string stdout;
            string stderr;
            var exitCode = Compiler.RunWithOutput(job.OutputDirectory.FullName, out stdout, out stderr, "mvn", ["compile"]);
            if (exitCode != 0)
            {
                throw new Exception($"Failed to compile Java code: {stdout} {stderr}");
            }
            job.Output.WriteInfo($"{stdout}");
        }

        public void reset()
        {
            Terms.Clear();
            VisitedSet.Clear();
            Predicates.Clear();
            FreeEvents.Clear();
            TermOrder.Clear();
            OrderToTerm.Clear();
            PredicateBoundedTerm.Clear();
            PredicateOrder.Clear();
            Contradictions.Clear();
        }

        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
        {
            if (hint == null)
            {
                throw new Exception("No search space configuration provided");
            }

            var quantifiedEvents = new List<PEvent>();
            foreach (var qe in hint.Quantified)
            {
                quantifiedEvents.Add(qe.EventDecl);
            }

            if (quantifiedEvents.Count == 0)
            {
                throw new Exception($"No event being quantified in hint space: {hint.Name}");
            }

            PEvent configEvent = hint.ConfigEvent;
            Constants.PInferModeOn();
            PredicateStore.Initialize();
            FunctionStore.Initialize();
            CompilationContext ctx = new(job);
            Java.CompilationContext javaCtx = new(job);
            var eventDefSource = new EventDefGenerator(job, Java.Constants.EventDefnFileName, quantifiedEvents, configEvent).GenerateCode(javaCtx, globalScope);
            AggregateFunctions(hint);
            AggregateDefinedPredicates(hint);
            PopulateEnumCmpPredicates(globalScope);
            var i = 0;
            var termDepth = hint.TermDepth.Value;
            var indexType = PInferBuiltinTypes.Index;
            var indexFunc = new BuiltinFunction("index", Notation.Prefix, PrimitiveType.Event, indexType);
            foreach ((var eventInst, var order) in quantifiedEvents.Select((x, i) => (x, i))) {
                var eventAtom = new PEventVariable($"e{i}")
                {
                    Type = eventInst.PayloadType,
                    EventDecl = eventInst,
                    Order = order
                };
                var expr = new VariableAccessExpr(null, eventAtom);
                AddTerm(0, expr,  [eventAtom]);
                foreach (var (e, w) in TryMkTupleAccess(expr).Concat(TryMakeNamedTupleAccess(expr)))
                {
                    AddTerm(0, e, w);
                }
                var indexExpr = new FunCallExpr(null, indexFunc, [expr]);
                HashSet<IPExpr> es = [];
                es.Add(indexExpr);
                AddTerm(0, indexExpr, [eventAtom]);
                i += 1;
            }
            Console.WriteLine($"Generating terms with max depth {termDepth} ...");
            PopulateTerm(termDepth);
            if (VisitedSet.Count == 0)
            {
                throw new Exception("No terms generated");
            }
            Console.WriteLine($"Generating predicates ...");
            PopulatePredicate();
            MkEqComparison();
            CompiledFile terms = new($"{job.ProjectName}.terms.json");
            CompiledFile predicates = new($"{job.ProjectName}.predicates.json");
            JavaCodegen codegen = new(job, $"{ctx.ProjectName}.java", Predicates, VisitedSet, FreeEvents);
            IEnumerable<CompiledFile> compiledJavaSrc = codegen.GenerateCode(javaCtx, globalScope);
            WriteTerms(ctx, terms.Stream, codegen);
            WritePredicates(ctx, predicates.Stream, codegen);
            var javaCompiler = new JavaCompiler();
            javaCompiler.GenerateBuildScript(job);
            var templateCodegen = new PInferTemplateGenerator(job, quantifiedEvents, Predicates, VisitedSet, FreeEvents,
                                                                PredicateBoundedTerm, OrderToTerm, configEvent);
            Console.WriteLine($"Generated {VisitedSet.Count} terms and {Predicates.Count} predicates");
            return compiledJavaSrc.Concat(new TraceReaderGenerator(job, quantifiedEvents.Concat([configEvent])).GenerateCode(javaCtx, globalScope))
                                .Concat(templateCodegen.GenerateCode(javaCtx, globalScope))
                                .Concat(new DriverGenerator(job, templateCodegen.TemplateNames).GenerateCode(javaCtx, globalScope))
                                .Concat(new PInferTypesGenerator(job, Constants.TypesDefnFileName).GenerateCode(javaCtx, globalScope))
                                .Concat(eventDefSource)
                                .Concat(new MinerConfigGenerator(job, quantifiedEvents.Count, VisitedSet.Count).GenerateCode(javaCtx, globalScope))
                                .Concat(new TaskPookGenerator(job).GenerateCode(javaCtx, globalScope))
                                .Concat(new FromDaikonGenerator(job, quantifiedEvents).GenerateCode(javaCtx, globalScope))
                                .Concat(new PredicateEnumeratorGenerator(job).GenerateCode(javaCtx, globalScope))
                                .Concat(new TermEnumeratorGenerator(job).GenerateCode(javaCtx, globalScope))
                                .Concat(new TemplateInstantiatorGenerator(job, quantifiedEvents.Count).GenerateCode(javaCtx, globalScope))
                                .Concat([terms, predicates]);
        }

        private void PopulateEnumCmpPredicates(Scope globalScope)
        {
            foreach (var enumDecl in globalScope.Enums)
            {
                var ty = new EnumType(enumDecl);
                var contraditionGroup = enumDecl.Name;
                List<MacroPredicate> predicateGroup = [];
                foreach (var elem in enumDecl.Values)
                {
                    MacroPredicate enumCmpPred = new($"enumCmp_{enumDecl.Name}_{elem.Name}", Notation.Prefix, (args, types, names) => {
                        return $"({args[0]} == {Constants.TypesNamespaceName}.{enumDecl.Name}.{elem.Name})";
                    }, ty);
                    PredicateStore.AddBuiltinPredicate(enumCmpPred, predicateGroup);
                    predicateGroup.Add(enumCmpPred);
                }
            }
            MacroPredicate isTrueCmp = new("IsTrue", Notation.Prefix, (args, types, names) => {
                return $"({args[0]})";
            }, PrimitiveType.Bool);
            MacroPredicate isFalseCmp = new("IsFalse", Notation.Prefix, (args, types, names) => {
                return $"(!{args[0]})";
            }, PrimitiveType.Bool);
            PredicateStore.AddBuiltinPredicate(isTrueCmp, [isFalseCmp]);
            PredicateStore.AddBuiltinPredicate(isFalseCmp, [isTrueCmp]);
        }

        private string GetEventVariableRepr(PEventVariable x)
        {
            return $"({x.Name}:{x.EventName})";
        }

        private void WritePredicates(CompilationContext ctx, StringWriter stream, JavaCodegen codegen)
        {
            ctx.WriteLine(stream, "[");
            foreach ((var pred, var index) in Predicates.Select((x, i) => (x, i)))
            {
                ctx.WriteLine(stream, "{");
                ctx.WriteLine(stream, $"\"order\": {PredicateOrder[pred]},");
                ctx.WriteLine(stream, $"\"repr\": \"{codegen.GenerateRawExpr(pred, true)}\", ");
                ctx.WriteLine(stream, $"\"terms\": [{string.Join(", ", PredicateBoundedTerm[pred])}], ");
                var comparer = new ASTComparer();
                if (Contradictions.TryGetValue(pred, out var contradictions))
                {
                    ctx.WriteLine(stream, $"\"contradictions\": [{string.Join(", ", contradictions.Where(PredicateOrder.ContainsKey).Select(x => PredicateOrder[x]))}]");
                }
                else
                {
                    ctx.WriteLine(stream, $"\"contradictions\": []");
                }
                ctx.WriteLine(stream, "}");
                if (index < Predicates.Count - 1)
                {
                    ctx.WriteLine(stream, ",");
                }
            }
            ctx.WriteLine(stream, "]");
        }

        private void WriteTerms(CompilationContext ctx, StringWriter stream, JavaCodegen codegen)
        {
            ctx.WriteLine(stream, "[");
            foreach ((var term, var index) in VisitedSet.Select((x, i) => (x, i)))
            {
                ctx.WriteLine(stream, "{");
                ctx.WriteLine(stream, $"\"order\": {TermOrder[term]}, ");
                ctx.WriteLine(stream, $"\"repr\": \"{codegen.GenerateRawExpr(term, true)}\",");
                ctx.WriteLine(stream, $"\"events\": [{string.Join(", ", FreeEvents[term].Select(x => $"{x.Order}"))}],");
                ctx.WriteLine(stream, $"\"type\": \"{codegen.GenerateTypeName(term)}\"");
                ctx.WriteLine(stream, "}");
                if (index < VisitedSet.Count - 1)
                {
                    ctx.WriteLine(stream, ", ");
                }
            }
            ctx.WriteLine(stream, "]");
        }

        private PLanguageType ExplicateTypeDef(PLanguageType type)
        {
            if (type is TypeDefType typedef)
            {
                if (typedef.TypeDefDecl.Type is not PrimitiveType) {
                    return typedef.TypeDefDecl.Type;
                }
            }
            return type;
        }

        private void PopulatePredicate()
        {
            var allTerms = VisitedSet;
            // Set of parameters types --> List of mappings of types and exprs
            var combinationCache = new Dictionary<HashSet<string>,
                                                  IDictionary<string, HashSet<IPExpr>>>();
            foreach (var pred in PredicateStore.Store)
            {
                var sig = pred.Signature.ParameterTypes.Select(ShowType).ToHashSet();
                var sigList = pred.Signature.ParameterTypes.ToList();
                var sigStrList = sigList.Select(ShowType).ToList();
                if (!combinationCache.TryGetValue(sig, out IDictionary<string, HashSet<IPExpr>> varMap))
                {
                    // Console.WriteLine($"Generating combinations for {pred.Name} => {string.Join(", ", sigStrList)}");
                    var parameterCombs = GetParameterCombinations(sigList.Count, allTerms, sigList, [], 0);
                    varMap = new Dictionary<string, HashSet<IPExpr>>();
                    combinationCache.Add(sig, varMap);
                    foreach (var parameters in parameterCombs)
                    {
                        foreach (var i in Enumerable.Range(0, parameters.Count))
                        {
                            if (!varMap.TryGetValue(sigStrList[i], out HashSet<IPExpr> exprs))
                            {
                                exprs = [];
                                varMap.Add(sigStrList[i], exprs);
                            }
                            varMap[sigStrList[i]].Add(parameters[i]);
                        }
                    }
                    combinationCache[sig] = varMap;
                }
                foreach (var parameters in CartesianProduct(sigList, varMap))
                {
                    var events = GetUnboundedEventsMultiple(parameters.ToArray());
                    var param = parameters.ToList();
                    if (PredicateCallExpr.MkPredicateCall(pred, param, out PredicateCallExpr expr)){
                        FreeEvents[expr] = events;
                        PredicateOrder[expr] = Predicates.Count;
                        Contradictions[expr] = new HashSet<IPExpr>(new ASTComparer());
                        foreach (var c in PredicateStore.GetContradictions(pred))
                        {
                            if (PredicateCallExpr.MkPredicateCall(c, param, out var contra))
                            {
                                Contradictions[expr].Add(contra);
                                if (!Contradictions.ContainsKey(contra))
                                {
                                    Contradictions[contra] = new HashSet<IPExpr>(new ASTComparer());
                                }
                                Contradictions[contra].Add(expr);
                            }
                        }
                        Predicates.Add(expr);
                        PredicateBoundedTerm[expr] = parameters.Select(x => TermOrder[x]).ToHashSet();
                    }
                }
            }
        }

        private IEnumerable<IEnumerable<IPExpr>> CartesianProduct(List<PLanguageType> types, IDictionary<string, HashSet<IPExpr>> varMaps, int termOrder = 0)
        {
            if (types.Count == 0 || varMaps.Count == 0)
            {
                yield break;
            }
            // TODO: fix canonicity
            // Maintain `order` for each type
            // Term order should be defined as the maximum `event order` of bounded events instead of
            // ids of terms
            if (types.Count == 1)
            {
                foreach (var e in varMaps[ShowType(types[0])].Where(x => TermOrder[x] >= termOrder))
                {
                    yield return [e];
                }
            }
            else
            {
                foreach (var e in varMaps[ShowType(types[0])])
                {
                    foreach (var rest in CartesianProduct(types.Skip(1).ToList(), varMaps, TermOrder[e]))
                    {
                        yield return rest.Prepend(e);
                    }
                }
            }
        }

        private void PopulateTerm(int currentDepth)
        {
            if (currentDepth == 0)
            {
                return;
            }
            else
            {
                PopulateTerm(currentDepth - 1);
                List<(IPExpr, HashSet<PEventVariable>)> worklist = [];
                foreach (var term in VisitedSet)
                {
                    // construct built-in exprs
                    var tupleAccess = TryMkTupleAccess(term);
                    var namedTupleAccess = TryMakeNamedTupleAccess(term);
                    worklist.AddRange(tupleAccess);
                    worklist.AddRange(namedTupleAccess);
                }
                foreach (var (expr, events) in worklist)
                {
                    AddTerm(currentDepth, expr, events);
                }
                // Map access
                foreach (var term in VisitedSet)
                {
                    if (term.Type is MapType)
                    {
                        var mapType = (MapType) term.Type;
                        var keyType = mapType.KeyType;
                        foreach (var key in VisitedSet.Where(x => IsAssignableFrom(keyType, x.Type)))
                        {
                            var mapAccess = new MapAccessExpr(null, term, key, mapType.ValueType);
                        }
                    }
                }
                // construct function calls
                worklist = [];
                foreach (Function func in FunctionStore.Store)
                {
                    // function calls
                    var sig = func.Signature;
                    var retType = sig.ReturnType;
                    // Console.WriteLine($"Generate for function: {func.Name} => {string.Join(", ", sig.Parameters.Select(x => ShowType(x.Type)).ToList())}");
                    var parameters = GetParameterCombinations(sig.Parameters.Count, VisitedSet, sig.Parameters.Select(x => x.Type).ToList(), []);
                    foreach (var parameter in parameters)
                    {
                        var expr = new FunCallExpr(null, func, parameter);
                        // AddTerm(currentDepth, expr, GetUnboundedEventsMultiple([.. parameter]));
                        worklist.Add((expr, GetUnboundedEventsMultiple([.. parameter])));
                    }
                }
                foreach (var (expr, events) in worklist)
                {
                    AddTerm(currentDepth, expr, events);
                }
            }
        }

        private bool IsEventVariableAccess(IPExpr expr)
        {
            // This function checks whether `expr` is an event variable access where the event is
            // a compound data type
            // Note: an event can carry only a primitive type (e.g. transaction id)
            return expr is VariableAccessExpr v && v.Variable is PEventVariable pv && pv.Type.Canonicalize() is not PrimitiveType;
        } 

        private void MkEqComparison()
        {
            var allTerms = VisitedSet.ToList();
            for (var i = 0; i < allTerms.Count; ++i)
            {
                for (var j = i + 1; j < allTerms.Count; ++j)
                {
                    // Console.WriteLine($"Comparing {JavaCodegen.GenerateCodeExpr(allTerms[i])}: {ShowType(allTerms[i].Type)} and {JavaCodegen.GenerateCodeExpr(allTerms[j])}: {ShowType(allTerms[j].Type)}: {IsAssignableFrom(allTerms[i].Type, allTerms[j].Type)}");
                    if (IsAssignableFrom(allTerms[i].Type, allTerms[j].Type)
                            && !IsEventVariableAccess(allTerms[i])
                            && !IsEventVariableAccess(allTerms[j]))
                    {
                        var lhs = allTerms[i];
                        var rhs = allTerms[j];
                        // var expr = new BinOpExpr(null, BinOpType.Eq, lhs, rhs);
                        if (PredicateCallExpr.MkEqualityComparison(lhs, rhs, out var expr))
                        {
                            if (!Contradictions.ContainsKey(expr))
                            {
                                Contradictions[expr] = new HashSet<IPExpr>(new ASTComparer());
                            }
                            if (PredicateCallExpr.MkPredicateCall("<", [lhs, rhs], out var c))
                            {
                                Contradictions[expr].Add(c);
                                if (!Contradictions.ContainsKey(c))
                                {
                                    Contradictions[c] = new HashSet<IPExpr>(new ASTComparer());
                                }
                                Contradictions[c].Add(expr);
                            }
                            PredicateOrder[expr] = Predicates.Count;
                            Predicates.Add(expr);
                            PredicateBoundedTerm[expr] = [TermOrder[lhs], TermOrder[rhs]];
                            FreeEvents[expr] = GetUnboundedEventsMultiple(lhs, rhs);
                        }
                    }
                }
            }
        }

        private IEnumerable<(IPExpr, HashSet<PEventVariable>)> TryMkTupleAccess(IPExpr tuple)
        {
            if (ExplicateTypeDef(tuple.Type) is TupleType tupleType)
            {
                for (var i = 0; i < tupleType.Types.Count; ++i)
                {
                    var expr = new TupleAccessExpr(null, tuple, i, tupleType.Types[i]);
                    yield return (expr, GetUnboundedEventsMultiple(tuple));
                }
            }
        }

        private IEnumerable<(IPExpr, HashSet<PEventVariable>)> TryMakeNamedTupleAccess(IPExpr tuple)
        {
            if (ExplicateTypeDef(tuple.Type) is NamedTupleType namedTupleType)
            {
                for (var i = 0; i < namedTupleType.Fields.Count; ++i)
                {
                    var expr = new NamedTupleAccessExpr(null, tuple, new NamedTupleEntry() {
                        Name = namedTupleType.Fields[i].Name,
                        Type = namedTupleType.Fields[i].Type,
                        FieldNo = i
                    });
                    yield return (expr, GetUnboundedEventsMultiple(tuple));
                }
            }
        }

        private IEnumerable<List<IPExpr>>
                GetParameterCombinations(int index, IEnumerable<IPExpr> candidateTerms, List<PLanguageType> declParams, List<IPExpr> parameters, int maxTermOrder = -1)
        {
            // Console.WriteLine($"declParams: {declParams} | index: {index}");
            if (index == 0)
            {
                List<IPExpr> copies = [];
                copies.AddRange(parameters);
                return [copies];
            }
            else
            {
                // Console.WriteLine($"Current params: {string.Join(" ", declParams)}");
                var declParam = declParams[0];
                List<IPExpr> newParameters = [];
                newParameters.AddRange(parameters);
                IEnumerable<List<IPExpr>> result = [];
                foreach (var expr in candidateTerms.Where(x => (maxTermOrder < 0 || TermOrder[x] >= maxTermOrder) && IsAssignableFrom(declParam, x.Type)))
                {
                    // Console.WriteLine($"Expr type: {ShowType(expr.Type)}, declParam: {ShowType(declParam)}, IsAssignable => {IsAssignableFrom(declParam, expr.Type)}, {candidateTerms.Where(x => IsAssignableFrom(x.Type, declParam)).Count()}");
                    newParameters.Add(expr);
                    result = result.Concat(GetParameterCombinations(index - 1, candidateTerms, declParams[1..], newParameters, maxTermOrder < 0 ? maxTermOrder : TermOrder[expr] + 1));
                    newParameters.RemoveAt(newParameters.Count - 1);
                }
                return result;
            }
        }

        private static bool GetFunction(string locator, Scope globalScope, out Function function)
        {
            if (globalScope.Get(locator, out function))
            {
                return true;
            }
            else if (locator.Contains('.'))
            {
                var split = locator.Split(".");
                if (globalScope.Get(split[0], out Machine m))
                {
                    foreach (var func in m.Methods)
                    {
                        if (func.Name == split[1])
                        {
                            function = func;
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private static void AggregateDefinedPredicates(Hint h) {
            foreach (var f in h.CustomPredicates)
            {
                // TODO handle the following either here or at PredicateStore
                // + Tautologies:
                //      - Reflexivity
                //      - Transitivity
                //      - Symmetry
                // + Subsumption
                // + Contradiction
                PredicateStore.AddPredicate(new DefinedPredicate(f), []);
            }
        }

        private static void AggregateFunctions(Hint h) {
            foreach (var f in h.CustomFunctions)
            {
                FunctionStore.AddFunction(f);
            }
        }

        private void AddTerm(int depth, IPExpr expr, HashSet<PEventVariable> unboundedEvents)
        {
            if (VisitedSet.Contains(expr))
            {
                return;
            }
            if (depth > Terms.Count)
            {
                throw new Exception($"Depth {depth} reached before reaching Depth {depth - 1}");
            }
            if (depth == Terms.Count)
            {
                Terms.Add([]);
            }
            if (!Terms[depth].ContainsKey(expr.Type))
            {
                Terms[depth].Add(expr.Type, []);
            }
            Terms[depth][expr.Type].Add(expr);
            FreeEvents[expr] = unboundedEvents;
            TermOrder[expr] = VisitedSet.Count;
            OrderToTerm[VisitedSet.Count] = expr;
            VisitedSet.Add(expr);
        }

        private static bool IsAssignableFrom(PLanguageType type, PLanguageType otherType)
        {
            // A slightly stricter version of type equality checking
            // when checking type aliases, we only regard aliases with the same name and 
            // they are referring to the same type
            if (type.Equals(PrimitiveType.Any))
            {
                return true;
            }
            if (type is TypeDefType || otherType is TypeDefType)
            {
                return (otherType is TypeDefType otherTypeDef) && (type is TypeDefType typedef)
                        && typedef.TypeDefDecl.Name == otherTypeDef.TypeDefDecl.Name
                        && typedef.TypeDefDecl.Type.Equals(otherTypeDef.TypeDefDecl.Type);
            }
            return type.IsAssignableFrom(otherType);
        }

        public static string ShowType(PLanguageType type)
        {
            return type switch
            {
                PrimitiveType primitiveType => primitiveType.CanonicalRepresentation,
                Index _ => "Index",
                TypeDefType typedef => $"{typedef.TypeDefDecl.Name}({ShowType(typedef.TypeDefDecl.Type)})",
                NamedTupleType namedTupleType => $"({string.Join(", ", namedTupleType.Types.Select(ShowType))})",
                TupleType tupleType => $"({string.Join(", ", tupleType.Types.Select(ShowType))})",
                SequenceType sequenceType => $"Seq<{ShowType(sequenceType.ElementType)}>",
                SetType setType => $"Set<{ShowType(setType.ElementType)}>",
                MapType mapType => $"Map<{ShowType(mapType.KeyType)}, {ShowType(mapType.ValueType)}>",
                _ => $"{type}",
            };
        }

        private HashSet<PEventVariable> GetUnboundedEventsMultiple(params IPExpr[] expr)
        {
            return expr.SelectMany(GetUnboundedEvents).ToHashSet();
        }

        private HashSet<PEventVariable> GetUnboundedEvents(IPExpr expr)
        {
            if (!FreeEvents.TryGetValue(expr, out HashSet<PEventVariable> unboundedEvents))
            {
                throw new Exception($"Expression {expr} has not been proceesed");
            }
            return unboundedEvents;
        }

        private List<Dictionary<PLanguageType, HashSet<IPExpr>>> Terms { get; }
        private HashSet<IPExpr> Predicates { get; }
        private HashSet<IPExpr> VisitedSet { get; }
        private Dictionary<IPExpr, HashSet<PEventVariable>> FreeEvents { get; }
        private Dictionary<IPExpr, int> TermOrder { get; }
        private Dictionary<int, IPExpr> OrderToTerm { get; }
        private Dictionary<IPExpr, int> PredicateOrder { get; }
        private Dictionary<IPExpr, HashSet<IPExpr>> Contradictions { get; }
        private Dictionary<IPExpr, HashSet<int>> PredicateBoundedTerm { get; }
        private IEnumerable<IPExpr> TermsAtDepth(int depth) => (depth < Terms.Count && depth >= 0) switch {
                                                                true => Terms[depth].Values.SelectMany(x => x),
                                                                false => []};
        private IEnumerable<IPExpr> AllTerms => Terms.SelectMany(x => x.Values).SelectMany(x => x);
        private IEnumerable<IPExpr> TermsAtDepthWithType(int depth, PLanguageType type) => (from ty in Terms[depth].Keys
                                                                                                where IsAssignableFrom(ty, type)
                                                                                                select Terms[depth][ty])
                                                                                            .SelectMany(x => x);
        private IEnumerable<IPExpr> TermsAtDepthSameTypeAs(int depth, IPExpr expr) => from term in TermsAtDepth(depth)
                                                                                            where IsAssignableFrom(term.Type, expr.Type)
                                                                                            select term;
    }
}
