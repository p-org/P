using System;
using System.Collections.Generic;
using System.Linq;
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
            globalScope.Get("eServerInit", out PEvent x);
            var ty = (NamedTupleType) x.PayloadType;
            job.Output.WriteInfo(
                $"Inspect eServerInit: {ty.Fields} | {string.Join(", ", ty.Types.Select(x => x.ToString()).ToList())}"
            );
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
            AggregateFunctions(globalScope);
            AggregateDefinedPredicates(globalScope);
            var i = 0;
            var termDepth = job.TermDepth.Value;
            Terms[0] = [];
            foreach (var eventInst in quantifiedEvents) {
                var eventAtom = new Variable($"e{i}", null, VariableRole.Temp)
                {
                    Type = eventInst.PayloadType
                };
                // Terms[0].Add(new VariableAccessExpr(null, eventAtom));
                AddTerm(0, new VariableAccessExpr(null, eventAtom));
            }
            throw new NotImplementedException();
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
                foreach (var term in TermsAtDepth(currentDepth - 1))
                {
                    // construct built-in exprs
                    TryMkTupleAccess(term, currentDepth);
                    TryMakeNamedTupleAccess(term, currentDepth);
                }
                foreach (Function func in FunctionStore.Store)
                {
                    // function calls
                    var sig = func.Signature;
                    var retType = sig.ReturnType;
                    var parameters = GetParameterCombinations(currentDepth -  1, 0, sig.Parameters.Select(x => x.Type).ToList(), []);
                    foreach (var parameter in parameters)
                    {
                        AddTerm(currentDepth, new FunCallExpr(null, func, parameter));
                    }
                }
            }
        }

        private void TryMkTupleAccess(IPExpr tuple, int depth)
        {
            if (tuple.Type is TupleType tupleType)
            {
                for (var i = 0; i < tupleType.Types.Count; ++i)
                {
                    AddTerm(depth, new TupleAccessExpr(null, tuple, i, tupleType.Types[i]));
                }
            }
        }

        private void TryMakeNamedTupleAccess(IPExpr tuple, int depth)
        {
            if (tuple.Type is NamedTupleType namedTupleType)
            {
                for (var i = 0; i < namedTupleType.Fields.Count; ++i)
                {
                    AddTerm(depth, new NamedTupleAccessExpr(null, tuple, new NamedTupleEntry() {
                        Name = namedTupleType.Fields[i].Name,
                        Type = namedTupleType.Fields[i].Type,
                        FieldNo = i
                    }));
                }
            }
        }

        private IEnumerable<List<IPExpr>>
                GetParameterCombinations(int depth, int index, List<PLanguageType> declParams, List<IPExpr> parameters)
        {
            if (declParams.Count == 0)
            {
                return [parameters];
            }
            else
            {
                var declParam = declParams[0];
                List<IPExpr> newParameters = [];
                newParameters.AddRange(parameters);
                IEnumerable<List<IPExpr>> result = [];
                foreach (var expr in TermsAtDepthWithType(depth, declParam))
                {
                    newParameters.Add(expr);
                    result = result.Concat(GetParameterCombinations(depth, index + 1, declParams.Skip(1).ToList(), newParameters));
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

        private void AddTerm(int depth, IPExpr expr)
        {
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
        }

        private List<Dictionary<PLanguageType, HashSet<IPExpr>>> Terms { get; }
        private IEnumerable<IPExpr> TermsAtDepth(int depth) => Terms[depth].Values.SelectMany(x => x);
        private IEnumerable<IPExpr> TermsAtDepthWithType(int depth, PLanguageType type) => (from ty in Terms[depth].Keys
                                                                                                where ty.IsAssignableFrom(type)
                                                                                                select Terms[depth][ty])
                                                                                            .SelectMany(x => x);
        private IEnumerable<IPExpr> TermsAtDepthSameTypeAs(int depth, IPExpr expr) => from term in TermsAtDepth(depth)
                                                                                            where term.Type.IsAssignableFrom(expr.Type)
                                                                                            select term;
    }
}