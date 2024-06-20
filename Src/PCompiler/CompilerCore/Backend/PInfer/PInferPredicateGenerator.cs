using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{
    public class PInferPredicateGenerator : JavaCompiler
    {

        public PInferPredicateGenerator()
        {
            Terms = [];
            VisitedSet = new HashSet<IPExpr>(new ASTComparer());
            Predicates = new HashSet<IPExpr>(new ASTComparer());
            FreeEvents = new Dictionary<IPExpr, HashSet<Variable>>(new ASTComparer());
        }

        public override IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
        {
            if (!job.TermDepth.HasValue)
            {
                throw new Exception("Term depth not specified for predicate enumeration");
            }
            if (job.QuantifiedEvents == null)
            {
                throw new Exception("Quantified events not specified for predicate enumeration");
            }

            var quantifiedEvents = new List<PEvent>();
            foreach (var ename in job.QuantifiedEvents)
            {
                if (globalScope.Get(ename, out PEvent e))
                {
                    quantifiedEvents.Add(e);
                }
                else
                {
                    throw new Exception($"Event {ename} not defined in global scope");
                }
            }
            Constants.PInferModeOn();
            PredicateStore.Initialize();
            FunctionStore.Initialize();
            CompilationContext ctx = new(job);
            Java.CompilationContext javaCtx = new(job);
            var eventDefSource = new EventDefGenerator(job, Java.Constants.EventDefnFileName, quantifiedEvents).GenerateCode(javaCtx, globalScope);
            AggregateFunctions(job.CustomFunctions, globalScope);
            AggregateDefinedPredicates(job.CustomPredicates, globalScope);
            var i = 0;
            var termDepth = job.TermDepth.Value;
            var indexType = PInferBuiltinTypes.Index;
            var indexFunc = new BuiltinFunction("index", Notation.Prefix, PrimitiveType.Event, indexType);
            foreach (var eventInst in quantifiedEvents) {
                var eventAtom = new PEventVariable($"e{i}")
                {
                    Type = ExplicateTypeDef(eventInst.PayloadType),
                    EventDecl = eventInst
                };
                var expr = new VariableAccessExpr(null, eventAtom);
                AddTerm(0, expr,  [eventAtom]);
                foreach (var (e, w) in TryMkTupleAccess(expr).Concat(TryMakeNamedTupleAccess(expr)))
                {
                    AddTerm(0, e, w);
                }
                var indexExpr = new FunCallExpr(null, indexFunc, [expr]);
                var indexExpr1 = new FunCallExpr(null, indexFunc, [expr]);
                HashSet<IPExpr> es = [];
                es.Add(indexExpr);
                AddTerm(0, indexExpr, [eventAtom]);
                i += 1;
            }
            Console.WriteLine($"Generating terms with max depth {termDepth} ...");
            PopulateTerm(termDepth);
            Console.WriteLine($"Generating predicates ...");
            PopulatePredicate();
            MkEqComparison();
            CompiledFile terms = new($"{job.ProjectName}.terms");
            CompiledFile predicates = new($"{job.ProjectName}.predicates");
            JavaCodegen codegen = new(job, $"{ctx.ProjectName}.java", Predicates, FreeEvents);
            foreach (var term in VisitedSet)
            {
                ctx.WriteLine(terms.Stream, codegen.GenerateRawExpr(term));
            }
            foreach (var pred in Predicates)
            {
                ctx.WriteLine(predicates.Stream, codegen.GenerateRawExpr(pred));
            }
            GenerateBuildScript(job);
            Console.WriteLine($"Generated {VisitedSet.Count} terms and {Predicates.Count} predicates");
            return codegen.GenerateCode(javaCtx, globalScope)
                            .Concat(new PInferTypesGenerator(job, Constants.TypesDefnFileName).GenerateCode(javaCtx, globalScope))
                            .Concat(eventDefSource)
                            .Concat([terms, predicates]);
        }

        private PLanguageType ExplicateTypeDef(PLanguageType type)
        {
            if (type is TypeDefType typedef)
            {
                return typedef.TypeDefDecl.Type;
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
                    var parameterCombs = GetParameterCombinations(sigList.Count, allTerms, sigList, []);
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
                    if (PredicateCallExpr.MkPredicateCall(pred, parameters.ToList(), out PredicateCallExpr expr)){
                        FreeEvents[expr] = events;
                        Predicates.Add(expr);
                    }
                }
            }
        }

        private IEnumerable<IEnumerable<IPExpr>> CartesianProduct(List<PLanguageType> types, IDictionary<string, HashSet<IPExpr>> varMaps)
        {
            if (varMaps.Count == 0)
            {
                return [];
            }
            if (types.Count == 0)
            {
                return [[]];
            }
            else
            {
                return from e in varMaps[ShowType(types[0])]
                        from rest in CartesianProduct(types.Skip(1).ToList(), varMaps)
                        select rest.Prepend(e);
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
                List<(IPExpr, HashSet<Variable>)> worklist = [];
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
            return expr is VariableAccessExpr v && v.Variable is PEventVariable;
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
                        var expr = new BinOpExpr(null, BinOpType.Eq, lhs, rhs);
                        Predicates.Add(expr);
                        FreeEvents[expr] = GetUnboundedEventsMultiple(lhs, rhs);
                    }
                }
            }
        }

        private IEnumerable<(IPExpr, HashSet<Variable>)> TryMkTupleAccess(IPExpr tuple)
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

        private IEnumerable<(IPExpr, HashSet<Variable>)> TryMakeNamedTupleAccess(IPExpr tuple)
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

        private static IEnumerable<List<IPExpr>>
                GetParameterCombinations(int index, IEnumerable<IPExpr> candidateTerms, List<PLanguageType> declParams, List<IPExpr> parameters)
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
                foreach (var expr in candidateTerms.Where(x => IsAssignableFrom(x.Type, declParam)))
                {
                    // Console.WriteLine($"Expr type: {ShowType(expr.Type)}, declParam: {ShowType(declParam)}, IsAssignable => {IsAssignableFrom(expr.Type, declParam)}, {candidateTerms.Where(x => IsAssignableFrom(x.Type, declParam)).Count()}");
                    newParameters.Add(expr);
                    result = result.Concat(GetParameterCombinations(index - 1, candidateTerms, declParams[1..], newParameters));
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

        private static void AggregateDefinedPredicates(List<string> predicates, Scope globalScope) {
            foreach (var name in predicates)
            {
                if (GetFunction(name, globalScope, out Function pred))
                {
                    if (pred.Signature.ReturnType.Equals(PrimitiveType.Bool))
                    {
                        // PredicateStore.Store.Add(new DefinedPredicate(pred));
                        PredicateStore.AddPredicate(new DefinedPredicate(pred));
                    }
                    else
                    {
                        throw new Exception($"Predicate {name} should have return type `bool` ({pred.Signature.ReturnType} is returned)");
                    }
                }
                else
                {
                    throw new Exception($"Predicate {name} is not defined");
                }
            }
        }

        private static void AggregateFunctions(List<string> functions, Scope globalScope) {
            foreach (var name in functions)
            {
                if (globalScope.Get(name, out Function function))
                {
                    FunctionStore.AddFunction(function);
                }
                else
                {
                    throw new Exception($"Function {name} not found");
                }
            }
        }

        private void AddTerm(int depth, IPExpr expr, HashSet<Variable> unboundedEvents)
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
            VisitedSet.Add(expr);
        }

        private static bool IsAssignableFrom(PLanguageType type, PLanguageType otherType)
        {
            // A slightly stricter version of type equality checking
            // when checking type aliases, we only regard aliases with the same name and 
            // they are referring to the same type
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
                _ => $"{type}",
            };
        }

        private HashSet<Variable> GetUnboundedEventsMultiple(params IPExpr[] expr)
        {
            return expr.SelectMany(GetUnboundedEvents).ToHashSet();
        }

        private HashSet<Variable> GetUnboundedEvents(IPExpr expr)
        {
            if (!FreeEvents.TryGetValue(expr, out HashSet<Variable> unboundedEvents))
            {
                throw new Exception($"Expression {expr} has not been proceesed");
            }
            return unboundedEvents;
        }

        private List<Dictionary<PLanguageType, HashSet<IPExpr>>> Terms { get; }
        private HashSet<IPExpr> Predicates { get; }
        private HashSet<IPExpr> VisitedSet { get; }
        private Dictionary<IPExpr, HashSet<Variable>> FreeEvents { get; }
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