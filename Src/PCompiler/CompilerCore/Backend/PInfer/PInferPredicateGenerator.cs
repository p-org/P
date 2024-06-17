using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.Expressions;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.PInfer
{
    public class PInferPredicateGenerator : ICodeGenerator
    {

        public PInferPredicateGenerator()
        {
            Terms = [];
            VisitedSet = new HashSet<IPExpr>(new ASTComparer());
            Predicates = new HashSet<IPExpr>(new ASTComparer());
            FreeEvents = [];
        }

        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
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
            CompilationContext ctx = new(job);
            AggregateFunctions(globalScope);
            AggregateDefinedPredicates(globalScope);
            var i = 0;
            var termDepth = job.TermDepth.Value;
            var indexType = PInferBuiltinTypes.Index;
            var indexFunc = new BuiltinFunction("index", Notation.Prefix, PrimitiveType.Event, indexType);
            // PredicateStore.AddBuiltinPredicate("<", Notation.Prefix, indexType, indexType);
            foreach (var eventInst in quantifiedEvents) {
                var eventAtom = new PEventVariable($"e{i}", eventInst.Name)
                {
                    Type = ExplicateTypeDef(eventInst.PayloadType)
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
            PopulateTerm(termDepth);
            PopulatePredicate();
            MkEqComparison();
            CompiledFile fp = new(ctx.FileName);
            CompiledFile terms = new($"{job.ProjectName}.terms");
            CompiledFile predicates = new($"{job.ProjectName}.predicates");
            foreach (var pred in PredicateStore.Store)
            {
                ctx.WriteLine(fp.Stream, GeneratePredicateDefn(pred));
            }
            foreach (var term in VisitedSet)
            {
                WriteToFile(term, ctx, terms.Stream);
            }
            foreach (var pred in Predicates)
            {
                WriteToFile(pred, ctx, predicates.Stream);
            }
            Console.WriteLine($"Generated {VisitedSet.Count} terms and {Predicates.Count} predicates");
            return [fp, terms, predicates];
        }

        private void WriteToFile(IPExpr expr, CompilationContext ctx, StringWriter fp)
        {
            var events = FreeEvents[expr].Select(x => {
                    var e = (PEventVariable) x;
                    return $"({e.Name}:{e.EventName})";
            });
            var code = GenerateCodeExpr(expr, ctx) + " where " + string.Join(" ", events);
            ctx.WriteLine(fp, code);
        }

        private string GeneratePredicateDefn(IPredicate predicate)
        {
            if (predicate is BuiltinPredicate)
            {
                // built-in predicates will be defined as static functions
                // inside the generated Java class.
                return "";
            }
            else
            {
                var bodyCode = GenerateCodeFuncBody(predicate.Function);
                if (predicate.Signature.Parameters.Count <= 3)
                {
                    var paramDefs = "(" + string.Join(", ", (from v in predicate.Signature.Parameters select GenerateCodeVariable(v)).ToArray()) + ")";
                    return $"{paramDefs} -> {{ {bodyCode} }}";
                }
                else
                {
                    throw new Exception("Predicate with more than three parameters is pending to be supported");
                }
            }
        }

        private PLanguageType ExplicateTypeDef(PLanguageType type)
        {
            if (type is TypeDefType typedef)
            {
                return typedef.TypeDefDecl.Type;
            }
            return type;
        }

        private string GenerateCodeExpr(IPExpr expr, CompilationContext ctx)
        {
            if (expr is VariableAccessExpr v)
            {
                return GenerateCodeVariable(v.Variable);
            }
            else if (expr is FunCallExpr f)
            {
                return GenerateFuncCall(f, ctx);
            }
            else if (expr is PredicateCallExpr p)
            {
                return GenerateCodePredicateCall(p, ctx);
            }
            else if (expr is TupleAccessExpr t)
            {
                return GenerateCodeTupleAccess(t, ctx);
            }
            else if (expr is NamedTupleAccessExpr n)
            {
                return GenerateCodeNamedTupleAccess(n, ctx);
            }
            else if (expr is BinOpExpr binOpExpr)
            {
                var lhs = GenerateCodeExpr(binOpExpr.Lhs, ctx);
                var rhs = GenerateCodeExpr(binOpExpr.Rhs, ctx);
                return binOpExpr.Operation switch
                {
                    BinOpType.Add => $"+(({lhs}) ({rhs}))",
                    BinOpType.Sub => $"-(({lhs}) ({rhs}))",
                    BinOpType.Mul => $"*(({lhs}) ({rhs}))",
                    BinOpType.Div => $"/(({lhs}) ({rhs}))",
                    BinOpType.Mod => $"%(({lhs}) ({rhs}))",
                    BinOpType.Eq => $"==(({lhs}) ({rhs}))",
                    BinOpType.Lt => $"<(({lhs}) ({rhs}))",
                    BinOpType.Gt => $">(({lhs}) ({rhs}))",
                    BinOpType.And => $"&&(({lhs}) ({rhs}))",
                    BinOpType.Or => $"||(({lhs}) ({rhs}))",
                    _ => throw new Exception($"Unsupported BinOp Operatoion: {binOpExpr.Operation}"),
                };
            }
            else
            {
                throw new Exception($"Unsupported expression type {expr.GetType()}");
            }
        }

        private static string GenerateCodeCall(string callee, params string[] args)
        {
            return $"{callee}({string.Join(", ", args)})";
        }

        private string GenerateCodeTupleAccess(TupleAccessExpr t, CompilationContext ctx)
        {
            return $"{GenerateCodeExpr(t.SubExpr, ctx)}[{t.FieldNo}]";
        }

        private string GenerateCodeNamedTupleAccess(NamedTupleAccessExpr n, CompilationContext ctx)
        {
            return $"{GenerateCodeExpr(n.SubExpr, ctx)}.{n.FieldName}";
        }

        private string GenerateCodeFuncBody(Function func)
        {
            return "";
        }

        private string GenerateCodePredicateCall(PredicateCallExpr p, CompilationContext ctx)
        {
            if (p.Predicate is BuiltinPredicate)
            {
                switch (p.Predicate.Notation)
                {
                    case Notation.Infix:
                        return $"{GenerateCodeExpr(p.Arguments[0], ctx)} {p.Predicate.Name} {GenerateCodeExpr(p.Arguments[1], ctx)}";
                }
            }
            var args = (from e in p.Arguments select GenerateCodeExpr(e, ctx)).ToArray();
            return GenerateCodeCall(p.Predicate.Name, args);
        }

        private string GenerateFuncCall(FunCallExpr funCallExpr, CompilationContext ctx)
        {
            if (funCallExpr.Function is BuiltinFunction builtinFun)
            {
                switch (builtinFun.Notation)
                {
                    case Notation.Infix:
                        return $"({GenerateCodeExpr(funCallExpr.Arguments[0], ctx)} {builtinFun.Name} {GenerateCodeExpr(funCallExpr.Arguments[1], ctx)})";
                    default:
                        break;
                }
            }
            return $"{funCallExpr.Function.Name}(" + string.Join(", ", (from e in funCallExpr.Arguments select GenerateCodeExpr(e, ctx)).ToArray()) + ")";
        }

        private static string GenerateCodeVariable(Variable v)
        {
            return v.Name;
        }

        private void PopulatePredicate()
        {
            var allTerms = VisitedSet;
            // Set of parameters types --> List of mappings of types and exprs
            var combinationCache = new Dictionary<HashSet<PLanguageType>,
                                                  IDictionary<PLanguageType, HashSet<IPExpr>>>();
            foreach (var pred in PredicateStore.Store)
            {
                var sig = pred.Signature.ParameterTypes.ToHashSet();
                var sigList = pred.Signature.ParameterTypes.ToList();
                if (!combinationCache.TryGetValue(sig, out IDictionary<PLanguageType, HashSet<IPExpr>> varMap))
                {
                    var parameterCombs = GetParameterCombinations(0, allTerms, sigList, []);
                    varMap = new Dictionary<PLanguageType, HashSet<IPExpr>>();
                    combinationCache.Add(sig, varMap);
                    foreach (var parameters in parameterCombs)
                    {
                        foreach (var i in Enumerable.Range(0, parameters.Count))
                        {
                            if (!varMap.TryGetValue(sigList[i], out HashSet<IPExpr> exprs))
                            {
                                exprs = [];
                                varMap.Add(sigList[i], exprs);
                            }
                            varMap[sigList[i]].Add(parameters[i]);
                        }
                    }
                    combinationCache[sig] = varMap;
                }
                foreach (var parameters in CartesianProduct(sigList, varMap))
                {
                    var events = GetUnboundedEventsMultiple(parameters.ToArray());
                    var expr = new PredicateCallExpr(pred, parameters.ToList());
                    FreeEvents[expr] = events;
                    Predicates.Add(expr);
                }
            }
        }

        private IEnumerable<IEnumerable<IPExpr>> CartesianProduct(List<PLanguageType> types, IDictionary<PLanguageType, HashSet<IPExpr>> varMaps)
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
                return from e in varMaps[types[0]]
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
                foreach (var term in AllTerms)
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
                foreach (Function func in FunctionStore.Store)
                {
                    // function calls
                    var sig = func.Signature;
                    var retType = sig.ReturnType;
                    var parameters = GetParameterCombinations(0, AllTerms, sig.Parameters.Select(x => x.Type).ToList(), []);
                    foreach (var parameter in parameters)
                    {
                        var expr = new FunCallExpr(null, func, parameter);
                        AddTerm(currentDepth, expr, GetUnboundedEventsMultiple([.. parameter]));
                    }
                }
            }
        }

        private void MkEqComparison()
        {
            var allTerms = VisitedSet.ToList();
            for (var i = 0; i < allTerms.Count; ++i)
            {
                for (var j = i + 1; j < allTerms.Count; ++j)
                {
                    if (IsAssignableFrom(allTerms[i].Type, allTerms[j].Type) && (allTerms[i] is not VariableAccessExpr) && (allTerms[j] is not VariableAccessExpr))
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
            if (tuple.Type is TupleType tupleType)
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
            if (tuple.Type is NamedTupleType namedTupleType)
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
            if (declParams.Count == 0)
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
                    newParameters.Add(expr);
                    result = result.Concat(GetParameterCombinations(index + 1, candidateTerms, declParams.Skip(1).ToList(), newParameters));
                    newParameters.RemoveAt(newParameters.Count - 1);
                }
                return result;
            }
        }

        private static void AggregateDefinedPredicates(Scope globalScope) {
            foreach (var f in globalScope.Functions)
            {
                if ((f.Role & FunctionRole.Predicate) == FunctionRole.Predicate && f.Signature.ReturnType == PrimitiveType.Bool)
                {
                    PredicateStore.Store.Add(new DefinedPredicate(f));
                }
            }
        }

        private static void AggregateFunctions(Scope globalScope) {
            foreach (var f in globalScope.Functions)
            {
                if ((f.Role & FunctionRole.Function) == FunctionRole.Function)
                {
                    FunctionStore.Store.Add(f);
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
            if (type is TypeDefType typedef)
            {
                return (otherType is TypeDefType otherTypeDef) 
                        && typedef.TypeDefDecl.Name == otherTypeDef.TypeDefDecl.Name
                        && typedef.TypeDefDecl.Type.Equals(otherTypeDef.TypeDefDecl.Type);
            }
            return type.IsAssignableFrom(otherType);
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