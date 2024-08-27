using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
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
        public int NumTerms = -1;
        public int NumPredicates = -1;
        private CongruenceClosure CC;
        private JavaCodegen Codegen;

        public PInferPredicateGenerator()
        {
            Terms = [];
            Comparer = new ASTComparer();
            VisitedSet = new HashSet<IPExpr>(Comparer);
            Predicates = new HashSet<IPExpr>(Comparer);
            FreeEvents = new Dictionary<IPExpr, HashSet<PEventVariable>>(Comparer);
            TermOrder = new Dictionary<IPExpr, int>(Comparer);
            OrderToTerm = new Dictionary<int, IPExpr>();
            ReprToTerms = new Dictionary<string, IPExpr>();
            ReprToPredicates = new Dictionary<string, IPExpr>();
            PredicateBoundedTerm = new Dictionary<IPExpr, HashSet<int>>(Comparer);
            PredicateOrder = new Dictionary<IPExpr, int>(Comparer);
            Contradictions = new Dictionary<IPExpr, HashSet<IPExpr>>(Comparer);
            CC = new();
            // should not cleared by reset, holds globally
            ReprToContradictions = [];
        }

        public void WithHint(Hint h)
        {
            hint = h;
        }

        public int GetPredicateId(IPExpr expr, ICompilerConfiguration job)
        {
            if (PredicateOrder.ContainsKey(expr))
            {
                return PredicateOrder[expr];
            }
            JavaCodegen codegen = new(job, "", Predicates, VisitedSet, FreeEvents);
            throw new Exception($"{codegen.GenerateCodeExpr(expr, true)} not in enumerated predicates!");
        }

        public int MaxArity()
        {
            int result = 1;
            foreach (var p in Predicates)
            {
                result = Math.Max(result, PredicateBoundedTerm[p].Count);
            }
            return result;
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
            // job.Output.WriteInfo($"{stdout}");
        }

        public void Reset()
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
            ReprToTerms.Clear();
            ReprToPredicates.Clear();
            CC.Reset();
            NumTerms = -1;
            NumPredicates = -1;
            PredicateStore.Reset();
            FunctionStore.Reset();
        }

        private void DecideAndAddCompPreidcatesFor(Scope globalScope, PLanguageType t)
        {
            if (PrimitiveType.Int.IsAssignableFrom(t) || PrimitiveType.Float.IsAssignableFrom(t))
            {
                PredicateStore.AddBinaryBuiltinPredicate(globalScope, BinOpType.Eq, t, t);
                if (globalScope.AllowedBinOpsByKind.ContainsKey(BinOpKind.Comparison))
                {
                    foreach (var types in globalScope.AllowedBinOpsByKind[BinOpKind.Comparison])
                    {
                        if (IsAssignableFrom(types.Item1, t) && IsAssignableFrom(types.Item2, t))
                        {
                            PredicateStore.AddBinaryBuiltinPredicate(globalScope, BinOpType.Lt, t, t);
                            break;
                        }
                    }
                }
                else
                {
                    PredicateStore.AddBinaryBuiltinPredicate(globalScope, BinOpType.Lt, t, t);
                }
            }
            if (t is NamedTupleType tupleType)
            {
                foreach (var entry in tupleType.Fields)
                {
                    DecideAndAddCompPreidcatesFor(globalScope, entry.Type);
                }
            }
            if (t is TypeDefType defType && !PredicateStore.HasDefinedPredicatesOver([t, t]))
            {
                DecideAndAddCompPreidcatesFor(globalScope, defType.TypeDefDecl.Type);
            }
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
            PredicateStore.Initialize(globalScope);
            FunctionStore.Initialize(globalScope);
            CompilationContext ctx = new(job);
            Java.CompilationContext javaCtx = new(job);
            DebugCodegen = new(job, "", Predicates, VisitedSet, FreeEvents);
            var eventDefSource = new EventDefGenerator(job, Constants.EventDefnFileName, quantifiedEvents, configEvent).GenerateCode(javaCtx, globalScope);
            AggregateFunctions(hint);
            AggregateDefinedPredicates(hint, globalScope);
            PopulateEnumCmpPredicates(globalScope);
            var i = 0;
            var termDepth = hint.TermDepth == null ? job.TermDepth : hint.TermDepth.Value;
            foreach (var eventAtom in hint.Quantified) {
                var expr = new VariableAccessExpr(null, eventAtom);
                AddTerm(0, expr, [eventAtom]);
                foreach (var (e, w) in TryMkTupleAccess(expr).Concat(TryMakeNamedTupleAccess(expr)))
                {
                    AddTerm(0, e, w);
                }
                var indexExpr = new FunCallExpr(null, BuiltinFunction.IndexOf, [expr]);
                HashSet<IPExpr> es = [];
                es.Add(indexExpr);
                AddTerm(0, indexExpr, [eventAtom]);
                DecideAndAddCompPreidcatesFor(globalScope, eventAtom.EventDecl.PayloadType);
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
            // MkEqComparison();

            CompiledFile terms = new($"{job.ProjectName}.terms.json");
            CompiledFile predicates = new($"{job.ProjectName}.predicates.json");
            JavaCodegen codegen = new(job, $"{ctx.ProjectName}.java", Predicates, VisitedSet, FreeEvents);
            Codegen = codegen;
            IEnumerable<CompiledFile> compiledJavaSrc = codegen.GenerateCode(javaCtx, globalScope);
            int numTerms = WriteTerms(ctx, terms.Stream, codegen);
            int numPredicates = WritePredicates(ctx, predicates.Stream, codegen);
            var javaCompiler = new JavaCompiler();
            javaCompiler.GenerateBuildScript(job);
            var templateCodegen = new PInferTemplateGenerator(job, quantifiedEvents, Predicates, VisitedSet, FreeEvents,
                                                                PredicateBoundedTerm, OrderToTerm, configEvent);
            Console.WriteLine($"Generated {numTerms} terms and {numPredicates} predicates");
            NumTerms = numTerms;
            NumPredicates = numPredicates;
            return compiledJavaSrc.Concat(new TraceReaderGenerator(job, quantifiedEvents.Concat([configEvent])).GenerateCode(javaCtx, globalScope))
                                .Concat(templateCodegen.GenerateCode(javaCtx, globalScope))
                                .Concat(new DriverGenerator(job, templateCodegen.TemplateNames).GenerateCode(javaCtx, globalScope))
                                .Concat(new PInferTypesGenerator(job, Constants.TypesDefnFileName).GenerateCode(javaCtx, globalScope))
                                .Concat(eventDefSource)
                                .Concat(new MinerConfigGenerator(job, quantifiedEvents.Count, VisitedSet.Count).GenerateCode(javaCtx, globalScope))
                                .Concat(new TaskPookGenerator(job).GenerateCode(javaCtx, globalScope))
                                .Concat(new FromDaikonGenerator(job, hint.Quantified).GenerateCode(javaCtx, globalScope))
                                .Concat(new PredicateEnumeratorGenerator(job).GenerateCode(javaCtx, globalScope))
                                .Concat(new TermEnumeratorGenerator(job).GenerateCode(javaCtx, globalScope))
                                .Concat(new TemplateInstantiatorGenerator(job, quantifiedEvents.Count).GenerateCode(javaCtx, globalScope))
                                .Concat([terms, predicates]);
        }

        private HashSet<PEventVariable> AddCustomTerm(ICompilerConfiguration job, IPExpr expr)
        {
            var cano = CC.Canonicalize(expr);
            if (cano != null)
            {
                if (!TermOrder.ContainsKey(cano))
                {
                    job.Output.WriteError($"{SimplifiedRepr(DebugCodegen, expr)} in CC but not added as a term");
                    Environment.Exit(1);
                }
                return FreeEvents[cano];
            }
            // term not present
            HashSet<PEventVariable> gather(IPExpr e) => AddCustomTerm(job, e);
            switch (expr)
            {
                case IntLiteralExpr:
                case BoolLiteralExpr:
                case EnumElemRefExpr:
                case FloatLiteralExpr:
                    return [];
                case BinOpExpr e:
                {
                    var fvl = gather(e.Lhs);
                    var fvr = gather(e.Rhs);
                    var n = fvl.Union(fvr).ToHashSet();
                    // depth does not matter now
                    AddTerm(-1, e, n);
                    return n;
                }
                case UnaryOpExpr e:
                {
                    AddTerm(-1, e, gather(e.SubExpr));
                    break;
                }
                case FunCallExpr funCallExpr:
                {
                    HashSet<PEventVariable> n = [];
                    foreach (var a in funCallExpr.Arguments)
                    {
                        n.UnionWith(gather(a));
                    }
                    AddTerm(-1, expr, n);
                    return n;
                }
                case NamedTupleAccessExpr namedTupleAccessExpr:
                {
                    var n = gather(namedTupleAccessExpr.SubExpr);
                    AddTerm(-1, expr, n);
                    return n;
                }
                case SizeofExpr sizeofExpr:
                {
                    var n = gather(sizeofExpr.Expr);
                    AddTerm(-1, expr, n);
                    return n;
                }
                case TupleAccessExpr tupleAccessExpr:
                {
                    var n = gather(tupleAccessExpr.SubExpr);
                    AddTerm(-1, expr, n);
                    return n;
                }
                default: break;
            }
            job.Output.WriteError($"Unsupported custom term: {SimplifiedRepr(DebugCodegen, expr)}");
            Environment.Exit(1);
            return null;
        }

        private void AddCustomPredicate(ICompilerConfiguration job, Scope globalScope, IPExpr expr)
        {
            if (CC.Canonicalize(expr) != null) return;
            switch (expr)
            {
                case BinOpExpr e:
                {
                    var freeEvents = AddCustomTerm(job, e.Lhs).Union(AddCustomTerm(job, e.Rhs)).ToHashSet();
                    var (lhs, rhs) = (CC.Canonicalize(e.Lhs), CC.Canonicalize(e.Rhs));
                    if (lhs == null || rhs == null)
                    {
                        throw new Exception($"Terms are not properly added: {SimplifiedRepr(DebugCodegen, e)}");
                    }
                    if (!PredicateStore.TryGetPredicate([lhs.Type, rhs.Type], PredicateStore.OpToName[e.Operation], out var pred))
                    {
                        pred = PredicateStore.AddBinaryBuiltinPredicate(globalScope, e.Operation, lhs.Type, rhs.Type, true);
                    }
                    AddPredicateCall(pred, [lhs, rhs]);
                    break;
                }
                case UnaryOpExpr e:
                {
                    var fv = AddCustomTerm(job, e.SubExpr);
                    var subexpr = CC.Canonicalize(e.SubExpr) ?? throw new Exception($"Terms are not properly added: {SimplifiedRepr(DebugCodegen, e)}");
                    switch (e.Operation)
                    {
                        case UnaryOpType.Not:
                        {
                            IPExpr pred = new UnaryOpExpr(e.SourceLocation, e.Operation, subexpr);
                            Predicates.Add(pred);
                            PredicateOrder[pred] = Predicates.Count;
                            PredicateBoundedTerm[pred] = [TermOrder[subexpr]];
                            FreeEvents[pred] = fv;
                            CC.AddExpr(pred, true);
                            break;
                        }
                        default: break;
                    }
                    break;
                }
                case FunCallExpr funCallExpr:
                {
                    Function callee = funCallExpr.Function;
                    if (!PredicateStore.TryGetPredicate(callee.Signature.ParameterTypes.ToList(), callee.Name, out var pred))
                    {
                        pred = new DefinedPredicate(funCallExpr.Function);
                        PredicateStore.AddPredicate(pred, []);
                    }
                    foreach (var arg in funCallExpr.Arguments)
                    {
                        AddCustomTerm(job, arg);
                    }
                    List<IPExpr> arguments = funCallExpr.Arguments.Select(CC.Canonicalize).ToList();
                    if (arguments.Any(x => x == null))
                    {
                        throw new Exception($"Terms are not properly added: {SimplifiedRepr(DebugCodegen, funCallExpr)}");
                    }
                    AddPredicateCall(pred, arguments);
                    break;
                }
                default: {
                    job.Output.WriteError($"Unsupported predicate hint: {SimplifiedRepr(DebugCodegen, expr)}");
                    Environment.Exit(1);
                    break;
                }
            }
        }

        private void AddHintPredicates(ICompilerConfiguration job, Scope globalScope, Hint h)
        {
            foreach (IPExpr pred in h.GuardPredicates.Concat(h.FilterPredicates))
            {
                AddCustomPredicate(job, globalScope, pred);
            }
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
                    MacroPredicate enumCmpPred = new($"enumCmp_{enumDecl.Name}_{elem.Name}", Notation.Prefix, (args) => {
                        return new BinOpExpr(null, BinOpType.Eq, args[0], new EnumElemRefExpr(null, elem));
                    }, ty);
                    enumCmpPred.Function.AddEquiv(
                        enumCmpPred.ShiftCall(xs => new BinOpExpr(null, BinOpType.Eq, xs[0], new IntLiteralExpr(null, elem.Value)))
                    );
                    foreach (var other in enumDecl.Values)
                    {
                        if (elem == other) continue;
                        enumCmpPred.Function.AddContradiction(
                            enumCmpPred.ShiftCall(xs => new BinOpExpr(null, BinOpType.Eq, xs[0], new EnumElemRefExpr(null, other)))
                        );
                    }
                    PredicateStore.AddPredicate(enumCmpPred, []);
                }
            }
            MacroPredicate isTrueCmp = new("IsTrue", Notation.Prefix, (args) => {
                return args[0];
            }, PrimitiveType.Bool);
            isTrueCmp.Function.AddContradiction(isTrueCmp.ShiftCall(
                xs => new UnaryOpExpr(null, UnaryOpType.Not, xs[0])
            ));
            MacroPredicate isFalseCmp = new("IsFalse", Notation.Prefix, (args) => {
                return new UnaryOpExpr(null, UnaryOpType.Not, args[0]);
            }, PrimitiveType.Bool);
            isFalseCmp.Function.AddContradiction(isFalseCmp.ShiftCall(xs => xs[0]));
            
            PredicateStore.AddBuiltinPredicate(isTrueCmp, []);
            PredicateStore.AddBuiltinPredicate(isFalseCmp, []);
        }

        private string GetEventVariableRepr(PEventVariable x)
        {
            return $"({x.Name}:{x.EventName})";
        }

        public static string SimplifiedRepr(JavaCodegen codegen, IPExpr expr)
        {
            return codegen.GenerateRawExpr(expr, true).Split("=>")[0].Trim();
        }

        private int WritePredicates(CompilationContext ctx, StringWriter stream, JavaCodegen codegen)
        {
            HashSet<IPExpr> written = new(Comparer);
            ctx.WriteLine(stream, "[");
            foreach ((var pred, var index) in Predicates.Select((x, i) => (x, i)))
            {
                var repr = SimplifiedRepr(codegen, pred);
                ReprToPredicates[repr] = pred;
                var canonical = CC.Canonicalize(pred);
                if (written.Contains(canonical))
                {
                    continue;
                }
                written.Add(canonical);
                ctx.WriteLine(stream, "{");
                ctx.WriteLine(stream, $"\"order\": {PredicateOrder[canonical]},");
                ctx.WriteLine(stream, $"\"repr\": \"{codegen.GenerateRawExpr(canonical, true)}\", ");
                ctx.WriteLine(stream, $"\"terms\": [{string.Join(", ", PredicateBoundedTerm[canonical])}], ");
                if (!ReprToContradictions.TryGetValue(repr, out var conReprs))
                {
                    conReprs = [];
                    ReprToContradictions[repr] = conReprs;
                }
                if (Contradictions.TryGetValue(canonical, out var contradictions))
                {
                    var cons = contradictions
                            .Select(x => {
                                var r = CC.Canonicalize(x);
                                if (r == null) return x;
                                else return r;
                            }).ToList();
                    foreach (var c in cons)
                    {
                        conReprs.Add(SimplifiedRepr(codegen, c));
                    }
                    ctx.WriteLine(stream, $"\"contradictions\": [{string.Join(", ", cons
                                            .Where(PredicateOrder.ContainsKey)
                                            .Select(x => PredicateOrder[x]).ToHashSet())}]");
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
            return written.Count;
        }

        private int WriteTerms(CompilationContext ctx, StringWriter stream, JavaCodegen codegen)
        {
            HashSet<IPExpr> written = new(Comparer);
            ctx.WriteLine(stream, "[");
            foreach ((var term, var index) in VisitedSet.Select((x, i) => (x, i)))
            {
                ReprToTerms[SimplifiedRepr(codegen, term)] = term;
                var canonical = CC.Canonicalize(term);
                if (written.Contains(canonical))
                {
                    continue;
                }
                written.Add(canonical);
                ctx.WriteLine(stream, "{");
                ctx.WriteLine(stream, $"\"order\": {TermOrder[canonical]}, ");
                ctx.WriteLine(stream, $"\"repr\": \"{codegen.GenerateRawExpr(canonical, true)}\",");
                ctx.WriteLine(stream, $"\"events\": [{string.Join(", ", FreeEvents[canonical].Select(x => $"{x.Order}"))}],");
                ctx.WriteLine(stream, $"\"type\": \"{codegen.GenerateTypeName(canonical)}\"");
                ctx.WriteLine(stream, "}");
                if (index < VisitedSet.Count - 1)
                {
                    ctx.WriteLine(stream, ", ");
                }
            }
            ctx.WriteLine(stream, "]");
            return written.Count;
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
                    AddPredicateCall(pred, parameters);
                }
            }
        }

        public void AddPredicateCall(IPredicate pred, IEnumerable<IPExpr> parameters)
        {
            var events = GetUnboundedEventsMultiple(parameters.ToArray());
            var param = parameters.ToList();
            if ((pred.Function.Property.HasFlag(FunctionProperty.Reflexive) || pred.Function.Property.HasFlag(FunctionProperty.AntiReflexive))
                    && param[0] == param[1])
            {
                // skip reflexive/irreflexive: tautology and contradiction
                return;
            }
            if (PredicateCallExpr.MkPredicateCall(pred, param, out IPExpr expr)){
                if (CC.Canonicalize(expr) != null) return;
                FreeEvents[expr] = events;
                PredicateOrder[expr] = Predicates.Count;
                Predicates.Add(expr);
                CC.AddExpr(expr, true);
                PredicateBoundedTerm[expr] = parameters.Select(x => TermOrder[x]).ToHashSet();
                // Add equivalences based on annotations
                foreach (IPExpr equiv in FunctionStore.MakeEquivalences(pred.Function,
                                        (_, xs) => PredicateCallExpr.MkPredicateCall(pred, xs, out var eq) ? eq : null, [.. parameters]))
                {
                    if (equiv == null) continue;
                    CC.AddExpr(equiv, false);
                    CC.MarkEquivalence(expr, equiv);
                }
                foreach (IPExpr con in FunctionStore.MakeContradictions(pred.Function,
                                        (_, xs) => PredicateCallExpr.MkPredicateCall(pred, xs, out var con) ? con : null, [.. parameters]))
                {
                    if (con == null) continue;
                    AddContradictingPredicates(expr, con);
                }
            }
        }

        private IEnumerable<IEnumerable<IPExpr>> CartesianProduct(List<PLanguageType> types, IDictionary<string, HashSet<IPExpr>> varMaps)
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
                foreach (var e in varMaps[ShowType(types[0])])
                {
                    yield return [e];
                }
            }
            else
            {
                foreach (var e in varMaps[ShowType(types[0])])
                {
                    foreach (var rest in CartesianProduct(types.Skip(1).ToList(), varMaps))
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
                        if (func.Property.HasFlag(FunctionProperty.Reflexive) && parameter[0].Equals(parameter[1]))
                        {
                            continue;
                        }
                        var expr = new FunCallExpr(null, func, parameter);
                        if (CC.Canonicalize(expr) != null)
                        {
                            continue;
                        }
                        CC.AddExpr(expr, true);
                        foreach (var eq in FunctionStore.MakeEquivalences(func, (f, xs) => new FunCallExpr(null, f, xs), [.. parameter]))
                        {
                            CC.AddExpr(eq, false);
                            CC.MarkEquivalence(expr, eq);
                        }
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

        private void AddContradictingPredicates(IPExpr e1, IPExpr e2)
        {
            if (!Contradictions.ContainsKey(e1))
            {
                Contradictions[e1] = new(Comparer);
            }
            if (!Contradictions.ContainsKey(e2))
            {
                Contradictions[e2] = new(Comparer);
            }
            Contradictions[e1].Add(e2);
            Contradictions[e2].Add(e1);
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
                foreach (var expr in candidateTerms.Where(x => IsAssignableFrom(declParam, x.Type)))
                {
                    // Console.WriteLine($"Expr type: {ShowType(expr.Type)}, declParam: {ShowType(declParam)}, IsAssignable => {IsAssignableFrom(declParam, expr.Type)}, {candidateTerms.Where(x => IsAssignableFrom(x.Type, declParam)).Count()}");
                    newParameters.Add(expr);
                    result = result.Concat(GetParameterCombinations(index - 1, candidateTerms, declParams[1..], newParameters));
                    newParameters.RemoveAt(newParameters.Count - 1);
                }
                return result;
            }
        }



        private void AggregateDefinedPredicates(Hint h, Scope globalScope) {
            if (h.CustomPredicates.Count > 0)
            {
                foreach (var f in h.CustomPredicates)
                {
                    PredicateStore.AddPredicate(new DefinedPredicate(f), []);
                }
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
            CC.AddExpr(expr, true);
        }

        public static bool SameType(PLanguageType t1, PLanguageType t2)
        {
            return IsAssignableFrom(t1, t2) && IsAssignableFrom(t2, t1);
        }

        public static bool IsAssignableFrom(PLanguageType type, PLanguageType otherType)
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
                        && IsAssignableFrom(typedef.TypeDefDecl.Type, otherTypeDef.TypeDefDecl.Type);
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
                EnumType enumType => $"Enum<{enumType.EnumDecl.Name}>",
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
                if (expr is IntLiteralExpr || expr is FloatLiteralExpr || expr is EnumElemRefExpr || expr is BoolLiteralExpr)
                {
                    return [];
                }
                throw new Exception($"Expression {expr} has not been proceesed");
            }
            return unboundedEvents;
        }

        public enum PruningStatus
        {
            KEEP,
            DROP,
            STEPBACK,            
        }

        private bool TryParseBinOpExpr(string inv, out (string, string, string) result)
        {
            string[] supportedOps = ["∈", "==", "<=", ">=", "!=", "<", ">"];
            foreach (var op in supportedOps)
            {
                if (inv.Contains(op))
                {
                    var decomp = inv.Split(op);
                    result = (decomp[0].Trim(), op, decomp[1].Trim());
                    return true;
                }
            }
            result = ("", "", "");
            return false;
        }

        public bool TryParseExpr(string repr, out PParser.ExprContext ctx)
        {
            var fileStream = new AntlrInputStream(repr);
            var lexer = new PLexer(fileStream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new PParser(tokens);
            parser.RemoveErrorListeners();
            try
            {
                // Stage 1: use fast SLL parsing strategy
                parser.Interpreter.PredictionMode = PredictionMode.Sll;
                parser.ErrorHandler = new BailErrorStrategy();
                ctx = parser.expr();
                return true;
            }
            catch (Exception)
            {
                ctx = null;
                return false;
            }
        }

        public bool TryGetFromConfigEvent(string field, out NamedTupleEntry entry)
        {
            if (hint.ConfigEvent != null)
            {
                NamedTupleType ty = (NamedTupleType) hint.ConfigEvent.PayloadType;
                foreach (var f in ty.Fields)
                {
                    if (field == f.Name) {
                        entry = f;
                        return true;
                    }
                }
            }
            entry = null;
            return false;
        }

        public string UnfoldContainsEnum(string lhs, string rhs, EnumType enumType)
        {
            HashSet<int> values = rhs.Replace("{", "").Replace("}", "").Split(",").Select(x => int.Parse(x.Trim())).ToHashSet();
            List<string> refElem = [];
            foreach (var v in enumType.EnumDecl.Values)
            {
                if (values.Contains(v.Value))
                {
                    refElem.Add(v.Name);
                }
            }
            return "(" + string.Join(" || ", refElem.Select(x => $"{lhs} == {x}")) + ")";
        }

        public HashSet<string> GetContradictionsByRepr(string repr)
        {
            if (!ReprToContradictions.TryGetValue(repr, out var cons))
            {
                cons = [];
                ReprToContradictions[repr] = cons;
            }
            return cons;
        }

        public void MarkReprContradictions(string r1, string r2)
        {
            var r1cons = GetContradictionsByRepr(r1);
            var r2cons = GetContradictionsByRepr(r2);
            r1cons.Add(r2);
            r2cons.Add(r1);
        }

        public void UpdateMetadata(IPExpr expr)
        {
            switch (expr)
            {
                case BinOpExpr binOpExpr:
                {
                    var repr = SimplifiedRepr(Codegen, expr);
                    foreach (var cons in binOpExpr.GetContradictions())
                    {
                        MarkReprContradictions(repr, SimplifiedRepr(Codegen, cons));
                    }
                    break;
                }
                default: return;
            }
        }

        public PruningStatus ProcessBinOpExpr(ICompilerConfiguration config, Scope globalScope,
                                                string orig, string lhs, string op, string rhs, out string processed)
        {
            // check for things that may not parse
            if (op == "∈")
            {
                // prune out for Enums that have <= 3 possible values
                if (ReprToTerms.TryGetValue(lhs, out var term))
                {
                    if (term.Type is EnumType t)
                    {
                        int rhsCnt = rhs.Count(c => c == ',') + 1;
                        if (rhsCnt == t.EnumDecl.Values.Count())
                        {
                            processed = "";
                            config.Output.WriteWarning($"[Drop] all enum value are covered in `{lhs} in {rhs}`");
                            return PruningStatus.DROP;
                        }
                        processed = UnfoldContainsEnum(lhs, rhs, t);
                        return PruningStatus.KEEP;
                    }
                }
                config.Output.WriteWarning($"[StepBack] lhs={lhs} in rhs={rhs} where rhs is a set of constants");
                processed = $"{lhs} {op} {rhs}";
                return PruningStatus.STEPBACK;
            }
            // try parse and check
            if (TryParseExpr(orig, out var ctx))
            {
                InvExprVisitor visitor = new(config, globalScope, CC, hint.ConfigEvent, ReprToTerms);
                try
                {
                    IPExpr expr = visitor.Visit(ctx);
                    if (!PrimitiveType.Bool.IsAssignableFrom(expr.Type))
                    {
                        config.Output.WriteWarning($"[Drop] {orig} does not have a boolean type");
                        processed = "";
                        return PruningStatus.DROP;
                    }
                    UpdateMetadata(expr);
                    processed = SimplifiedRepr(Codegen, expr);
                    return PruningStatus.KEEP;
                }
                catch (DropException drop)
                {
                    config.Output.WriteWarning(drop.Message);
                    processed = "";
                    return PruningStatus.DROP;
                }
                catch (StepbackException sb)
                {
                    config.Output.WriteWarning(sb.Message);
                    processed = orig;
                    return PruningStatus.STEPBACK;
                }
            }
            processed = "";
            config.Output.WriteWarning($"[Drop] Cannot parse {orig}");
            return PruningStatus.DROP;
        }

        public PruningStatus CheckForPruning(ICompilerConfiguration config, Scope globalScope, string inv, out string repr)
        {
            if (ReprToPredicates.ContainsKey(inv))
            {
                repr = inv;
                return PruningStatus.KEEP;
            }
            if (TryParseBinOpExpr(inv, out var exprComp))
            {
                var lhs = exprComp.Item1;
                var op = exprComp.Item2;
                var rhs = exprComp.Item3;
                return ProcessBinOpExpr(config, globalScope, inv, lhs, op, rhs, out repr);
            }
            // drop unknown operators
            repr = "";
            config.Output.WriteWarning($"[Drop] Unknown operator in {inv}");
            return PruningStatus.DROP;
        }

        public bool Contradicting(string p, string q)
        {
            if (!ReprToContradictions.TryGetValue(p, out HashSet<string> c1) || !ReprToContradictions.TryGetValue(q, out HashSet<string> c2))
            {
                // over-approximate this by only checking known predicates
                return false;
            }
            return c1.Contains(q) || c2.Contains(p);
        }

        private List<Dictionary<PLanguageType, HashSet<IPExpr>>> Terms { get; }
        private HashSet<IPExpr> Predicates { get; }
        private HashSet<IPExpr> VisitedSet { get; }
        private Dictionary<IPExpr, HashSet<PEventVariable>> FreeEvents { get; }
        private Dictionary<IPExpr, int> TermOrder { get; }
        private Dictionary<int, IPExpr> OrderToTerm { get; }
        private Dictionary<IPExpr, int> PredicateOrder { get; }
        private Dictionary<string, IPExpr> ReprToTerms { get; }
        private Dictionary<string, IPExpr> ReprToPredicates { get; }
        private Dictionary<string, HashSet<string>> ReprToContradictions { get; }
        private Dictionary<IPExpr, HashSet<IPExpr>> Contradictions { get; }
        private Dictionary<IPExpr, HashSet<int>> PredicateBoundedTerm { get; }
        private JavaCodegen DebugCodegen { get; set; }
        private IEnumerable<IPExpr> TermsAtDepth(int depth) => (depth < Terms.Count && depth >= 0) switch {
                                                                true => Terms[depth].Values.SelectMany(x => x),
                                                                false => []};
        private IEqualityComparer<IPExpr> Comparer { get; }
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
